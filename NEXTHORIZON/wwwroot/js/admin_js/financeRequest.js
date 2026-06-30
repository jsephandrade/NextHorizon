﻿// Global variables
let currentRequestId = "";
let currentAction = "";
let currentAmount = 0;
let currentReviewId = "";
let currentProductId = ""; // Added for Product Approvals
let currentEditingPromoId = null;
let selectedImageFile = null;
let imageBase64Data = null;
let bannerImageName = null;
let bannerImageContentType = null;

document.getElementById('promoImage')?.addEventListener('change', function(e) {
    const file = e.target.files[0];
    if (file) {
        bannerImageName = file.name;
        bannerImageContentType = file.type;
        
        const reader = new FileReader();
        reader.onload = function(event) {
            imageBase64Data = event.target.result;
            const preview = document.getElementById('promoPreview');
            const container = document.getElementById('promoPreviewContainer');
            if (preview && container) {
                preview.src = event.target.result;
                preview.setAttribute('data-base64', event.target.result);
                container.style.display = 'block';
            }
        };
        reader.readAsDataURL(file);
    }
});

// Toggle end date field based on checkbox
function initPromoFormToggle() {
    const noEndDateCheckbox = document.getElementById('noEndDate');
    const promoEndInput = document.getElementById('promoEnd');
    
    if (noEndDateCheckbox && promoEndInput) {
        // Initial state
        promoEndInput.disabled = noEndDateCheckbox.checked;
        
        // Remove required attribute when checkbox is checked
        if (noEndDateCheckbox.checked) {
            promoEndInput.removeAttribute('required');
        } else {
            promoEndInput.setAttribute('required', 'required');
        }
        
        // Toggle on change
        noEndDateCheckbox.addEventListener('change', function() {
            promoEndInput.disabled = this.checked;
            if (this.checked) {
                promoEndInput.value = '';
                promoEndInput.classList.remove('is-invalid');
                promoEndInput.removeAttribute('required');
            } else {
                promoEndInput.setAttribute('required', 'required');
                promoEndInput.focus();
            }
        });
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Initialize promo form toggle
    initPromoFormToggle();
    
    // Load all pending lists
    loadPendingPayouts();
    loadGlobalPromotions();
    
    // Set min date for date inputs to today
    const today = new Date().toISOString().split('T')[0];
    const promoStart = document.getElementById('promoStart');
    const promoEnd = document.getElementById('promoEnd');
    if (promoStart) promoStart.min = today;
    if (promoEnd) promoEnd.min = today;

    // Delegation for Promo Buttons
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('edit-promo-btn')) {
            const promoId = parseInt(e.target.getAttribute('data-promo-id'));
            const promo = window.promotionsData?.find(p => p.id === promoId);
            if (promo) editPromo(promo);
        }
        if (e.target.classList.contains('delete-promo-btn')) {
            deletePromo(parseInt(e.target.getAttribute('data-promo-id')));
        }
    });
});

// Product Approval Modal Functions
function openProductApproveModal(productId, productName, productImage, productPrice, productCategory) {
    console.log('Product Image received:', productImage ? `Length: ${productImage.length}, Starts with: ${productImage.substring(0, 50)}` : 'No image');
    
    currentProductId = productId;
    
    // Hide reject content, show approve content
    document.getElementById('productRejectContent').style.display = 'none';
    document.getElementById('productApproveContent').style.display = 'block';
    document.getElementById('productConfirmApproveBtn').style.display = 'block';
    document.getElementById('productConfirmRejectBtn').style.display = 'none';
    document.getElementById('productActionTitle').innerText = 'Approve Product';
    
    // Use the image directly - it already has the data:image prefix from C#
    const imageSrc = productImage && productImage !== '' && productImage !== 'null' ? productImage : null;
    
    console.log('Image src to use:', imageSrc ? imageSrc.substring(0, 100) : 'null');
    
    // Create modern product preview HTML
    document.getElementById('productApproveMessage').innerHTML = `
        <div class="product-preview-modern">
            <div class="product-image-container text-center mb-4">
                ${imageSrc ? 
                    `<img src="${imageSrc}" alt="${escapeHtml(productName)}" class="product-preview-image rounded-3 shadow-sm" style="width: 100%; max-height: 200px; object-fit: cover;" 
                         onerror="console.log('Image load error'); this.src='/images/challenge-placeholder.jpg'">` : 
                    `<div class="product-placeholder bg-light rounded-3 d-flex flex-column align-items-center justify-content-center" style="height: 150px;">
                        <i class="bi bi-image fs-1 text-muted"></i>
                        <span class="small text-muted mt-2">No Image Available</span>
                    </div>`
                }
            </div>
            <div class="product-details text-center">
                <h5 class="fw-bold mb-2">${escapeHtml(productName)}</h5>
                ${productCategory ? `<p class="text-muted small mb-2"><i class="bi bi-tag me-1"></i>${escapeHtml(productCategory)}</p>` : ''}
                ${productPrice ? `<p class="h5 text-success fw-bold mb-3">₱${parseFloat(productPrice).toLocaleString()}</p>` : ''}
                <div class="alert alert-success rounded-3 d-flex align-items-center justify-content-center gap-2 mb-3">
                    <i class="bi bi-check-circle-fill"></i>
                    <span class="small">Are you sure you want to approve this product?</span>
                </div>
                <p class="text-muted small">
                    <i class="bi bi-info-circle"></i>
                    This product will become visible to customers immediately upon approval.
                </p>
            </div>
        </div>
    `;
    
    const modal = new bootstrap.Modal(document.getElementById('productActionModal'));
    modal.show();
}

function openProductRejectModalWithModal(productId, productName, productImage, productPrice, productCategory) {
    console.log('Reject modal - Product Image received:', productImage ? `Length: ${productImage.length}, Starts with: ${productImage.substring(0, 50)}` : 'No image');
    
    currentProductId = productId;
    
    // Hide approve content, show reject content
    document.getElementById('productApproveContent').style.display = 'none';
    document.getElementById('productRejectContent').style.display = 'block';
    document.getElementById('productConfirmApproveBtn').style.display = 'none';
    document.getElementById('productConfirmRejectBtn').style.display = 'block';
    document.getElementById('productActionTitle').innerText = 'Reject Product';
    
    // Use the image directly - it already has the data:image prefix from C#
    const imageSrc = productImage && productImage !== '' && productImage !== 'null' ? productImage : null;
    
    console.log('Reject modal - Image src to use:', imageSrc ? imageSrc.substring(0, 100) : 'null');
    
    // Create modern product preview for reject modal
    document.getElementById('productRejectMessage').innerHTML = `
        <div class="product-preview-modern">
            <div class="product-image-container text-center mb-4">
                ${imageSrc ? 
                    `<img src="${imageSrc}" alt="${escapeHtml(productName)}" class="product-preview-image rounded-3 shadow-sm" style="width: 100%; max-height: 200px; object-fit: cover;" 
                         onerror="console.log('Image load error'); this.src='/images/challenge-placeholder.jpg'">` : 
                    `<div class="product-placeholder bg-light rounded-3 d-flex flex-column align-items-center justify-content-center" style="height: 150px;">
                        <i class="bi bi-image fs-1 text-muted"></i>
                        <span class="small text-muted mt-2">No Image Available</span>
                    </div>`
                }
            </div>
            <div class="product-details text-center">
                <h5 class="fw-bold mb-2">${escapeHtml(productName)}</h5>
                ${productCategory ? `<p class="text-muted small mb-2"><i class="bi bi-tag me-1"></i>${escapeHtml(productCategory)}</p>` : ''}
                ${productPrice ? `<p class="h5 text-success fw-bold mb-3">₱${parseFloat(productPrice).toLocaleString()}</p>` : ''}
                <div class="alert alert-warning rounded-3 d-flex align-items-center justify-content-center gap-2 mb-3">
                    <i class="bi bi-exclamation-triangle-fill"></i>
                    <span class="small">Please provide a reason for rejecting this product:</span>
                </div>
            </div>
        </div>
    `;
    
    // Clear previous reason
    document.getElementById('productRejectReason').value = '';
    
    const modal = new bootstrap.Modal(document.getElementById('productActionModal'));
    modal.show();
}

// Core approval function
async function processProductApproval(productId, status, reason = "") {
    showToast('Updating product status...');
    
    try {
        const response = await fetch('/Admin/UpdateProductStatus', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                productId: productId, 
                action: status, 
                reason: reason 
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message);
            // Reload the page to refresh all data
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showToast(data.message || 'Error updating product', true);
        }
    } catch (err) {
        console.error('Error updating product:', err);
        showToast('Error updating product: ' + err.message, true);
    }
}

// Confirm product approval
async function confirmProductApprove() {
    if (!currentProductId) return;
    
    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('productActionModal'));
    modal.hide();
    
    // Process approval
    await processProductApproval(currentProductId, 'approve', '');
}

// Confirm product rejection
async function confirmProductReject() {
    if (!currentProductId) return;
    
    const reason = document.getElementById('productRejectReason').value.trim();
    
    if (reason === "") {
        showToast("Rejection reason is mandatory", true);
        return;
    }
    
    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('productActionModal'));
    modal.hide();
    
    // Process rejection with reason
    await processProductApproval(currentProductId, 'reject', reason);
}

// PAYOUT WORKFLOW
function openPayoutModal(id, amount, action) {
    currentRequestId = id;
    currentAction = action;
    currentAmount = amount;

    const title = document.getElementById('financeTitle');
    const msg = document.getElementById('financeMessage');
    const btn = document.getElementById('financeConfirmBtn');

    if (action === 'approve') {
        title.innerText = "Approve Payout";
        msg.innerText = `Release ₱${amount.toLocaleString()} for Request #${id}?`;
        btn.className = "btn btn-success w-100 rounded-pill";
    } else {
        title.innerText = "Reject Request";
        msg.innerText = `Mandatory reason required for Request #${id}:`;
        btn.className = "btn btn-danger w-100 rounded-pill";
    }

    btn.onclick = submitPayoutDecision;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('financeActionModal')).show();
}

async function submitPayoutDecision() {
    const note = document.getElementById('financeNote')?.value.trim();
    if (currentAction === 'reject' && !note) {
        document.getElementById('financeNote').classList.add('is-invalid');
        return;
    }

    bootstrap.Modal.getInstance(document.getElementById('financeActionModal'))?.hide();
    showToast('Processing...');

    try {
        const response = await fetch('/Admin/ProcessPayout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ withdrawalId: currentRequestId, action: currentAction, reason: note, amount: currentAmount })
        });
        const data = await response.json();
        if (data.success) {
            showToast(data.message);
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showToast(data.message, true);
        }
    } catch (err) { 
        showToast('Connection error', true); 
    }
}

async function loadPendingPayouts() {
    try {
        const response = await fetch('/Admin/GetPendingPayouts');
        const result = await response.json();
        const payouts = Array.isArray(result) ? result : (result.data || []);
        const tbody = document.querySelector('#payoutsTable tbody');
        if (!tbody) return;

        tbody.innerHTML = payouts.length === 0
            ? '<tr><td colspan="6" class="text-center py-4 text-muted">No pending payouts.</td></tr>'
            : payouts.map(p => `
                <tr>
                    <td class="ps-4 fw-bold">#${p.id}</td>
                    <td><span class="d-block fw-bold small">${p.shopName}</span></td>
                    <td><span class="text-success fw-bold">₱${p.amount.toLocaleString()}</span></td>
                    <td><span class="badge bg-light text-dark border">${p.bankName}</span></td>
                    <td class="text-center">
                        <button class="btn btn-sm btn-success rounded-pill px-3" onclick="openPayoutModal(${p.id}, ${p.amount}, 'approve')">Approve</button>
                        <button class="btn btn-sm btn-outline-danger rounded-pill px-3" onclick="openPayoutModal(${p.id}, ${p.amount}, 'reject')">Reject</button>
                    </td>
                </tr>
            `).join('');
    } catch (err) { console.error(err); }
}

// DISCOUNT REVIEW FUNCTIONS
function openDiscountReviewModal(discountId, productName, discountPercent, newPrice, createdAt, productImage) {
    currentReviewId = discountId;
    
    // Set modal content
    document.getElementById('discountProductName').innerText = productName;
    document.getElementById('discountDiscountPercent').innerText = `${discountPercent}%`;
    document.getElementById('discountNewPrice').innerText = `₱${newPrice.toLocaleString()}`;
    document.getElementById('discountCreatedAt').innerText = createdAt;
    
    if (productImage) {
        document.getElementById('discountProductImage').src = productImage;
        document.getElementById('discountProductImage').style.display = 'block';
    } else {
        document.getElementById('discountProductImage').style.display = 'none';
    }
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('discountReviewModal'));
    modal.show();
}

// Approve discount from modal
async function approveDiscount() {
    if (!currentReviewId) return;
    
    // Close modal
    const modal = bootstrap.Modal.getInstance(document.getElementById('discountReviewModal'));
    modal.hide();
    
    showToast('Processing discount approval...');
    
    try {
        const response = await fetch('/Admin/ProcessDiscount', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                discountId: currentReviewId, 
                action: 'approve',
                reason: ''
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message);
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showToast(data.message || 'Error approving discount', true);
        }
        
    } catch (err) {
        console.error('Error approving discount:', err);
        showToast('Error approving discount: ' + err.message, true);
    }
}

let currentDiscountId = null;

// Open discount reject modal
function openDiscountRejectModal(discountId) {
    currentDiscountId = discountId;
    
    // Clear previous input
    document.getElementById('discountRejectReason').value = '';
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('discountRejectModal'));
    modal.show();
}

// Confirm discount rejection
async function confirmDiscountReject() {
    if (!currentDiscountId) return;
    
    const reason = document.getElementById('discountRejectReason').value.trim();
    
    if (reason === "") {
        showToast("Rejection reason is mandatory", true);
        return;
    }
    
    // Close modal first
    const modal = bootstrap.Modal.getInstance(document.getElementById('discountRejectModal'));
    modal.hide();
    
    showToast('Processing discount rejection...');
    
    try {
        const response = await fetch('/Admin/ProcessDiscount', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                discountId: currentDiscountId, 
                action: 'reject',
                reason: reason
            })
        });
        
        const data = await response.json();
        
        if (data.success) {
            showToast(data.message);
            // Reload the page to refresh all data
            setTimeout(() => {
                location.reload();
            }, 1500);
        } else {
            showToast(data.message || 'Error rejecting discount', true);
        }
        
    } catch (err) {
        console.error('Error rejecting discount:', err);
        showToast('Error rejecting discount: ' + err.message, true);
    }
}

// Update openDiscountReviewModal to store the ID and add Reject button with modal
function openDiscountReviewModal(discountId, productName, discountPercent, newPrice, createdAt, productImage) {
    currentReviewId = discountId;
    
    // Set modal content
    document.getElementById('discountProductName').innerText = productName;
    document.getElementById('discountDiscountPercent').innerText = `${discountPercent}%`;
    document.getElementById('discountNewPrice').innerText = `₱${newPrice.toLocaleString()}`;
    document.getElementById('discountCreatedAt').innerText = createdAt;
    
    if (productImage && productImage !== '') {
        document.getElementById('discountProductImage').src = productImage;
        document.getElementById('discountProductImage').style.display = 'block';
    } else {
        document.getElementById('discountProductImage').style.display = 'none';
    }
    
    // Update reject button to use modal instead of prompt
    const rejectBtn = document.querySelector('#discountReviewModal .btn-outline-danger');
    if (rejectBtn) {
        rejectBtn.onclick = () => openDiscountRejectModal(discountId);
    }
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('discountReviewModal'));
    modal.show();
}

// GLOBAL PROMO LOGIC - UPDATED WITH VALIDATION
async function handleGlobalPromoSubmission(event) {
    event.preventDefault();
    
    // Get elements with null checks
    const noEndDateCheckbox = document.getElementById('noEndDate');
    const promoEndInput = document.getElementById('promoEnd');
    const promoPreview = document.getElementById('promoPreview');
    const promoTitle = document.getElementById('promoTitle');
    const promoPercent = document.getElementById('promoPercent');
    const promoStart = document.getElementById('promoStart');
    
    const isIndefinite = noEndDateCheckbox ? noEndDateCheckbox.checked : false;
    const endDate = (!isIndefinite && promoEndInput && promoEndInput.value) ? promoEndInput.value : null;
    
    // Validate end date if not indefinite
    if (!isIndefinite && (!endDate || endDate === '')) {
        showToast('Please select an end date or check "No end date" for permanent promotion', true);
        if (promoEndInput) {
            promoEndInput.classList.add('is-invalid');
            promoEndInput.focus();
        }
        return;
    }
    
    // Remove invalid class if valid
    if (promoEndInput) {
        promoEndInput.classList.remove('is-invalid');
    }
    
    const requestData = {
        id: currentEditingPromoId,
        name: promoTitle ? promoTitle.value.trim() : '',
        description: document.getElementById('promoDesc')?.value.trim() || '',
        discountPercent: promoPercent ? parseFloat(promoPercent.value) : 0,
        startDate: promoStart ? promoStart.value : '',
        endDate: endDate,
        isIndefinite: isIndefinite,
        bannerImageBase64: imageBase64Data || (promoPreview ? promoPreview.getAttribute('data-base64') : null),
        bannerImageName: bannerImageName || null,
        bannerImageContentType: bannerImageContentType || null
    };
    
    // Validation
    if (!requestData.name) {
        showToast('Please enter promotion title', true);
        if (promoTitle) promoTitle.focus();
        return;
    }
    
    if (!requestData.discountPercent || requestData.discountPercent <= 0) {
        showToast('Please enter valid discount percentage', true);
        if (promoPercent) promoPercent.focus();
        return;
    }
    
    if (requestData.discountPercent > 100) {
        showToast('Discount percentage cannot exceed 100%', true);
        if (promoPercent) promoPercent.focus();
        return;
    }
    
    if (!requestData.startDate) {
        showToast('Please select start date', true);
        if (promoStart) promoStart.focus();
        return;
    }
    
    // Validate that end date is after start date
    if (!isIndefinite && endDate) {
        const startDateObj = new Date(requestData.startDate);
        const endDateObj = new Date(endDate);
        
        if (endDateObj <= startDateObj) {
            showToast('End date must be after start date', true);
            if (promoEndInput) {
                promoEndInput.classList.add('is-invalid');
                promoEndInput.focus();
            }
            return;
        }
    }

    showToast('Saving promotion...');
    
    try {
        const resp = await fetch('/Admin/SaveGlobalPromotion', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestData)
        });
        const res = await resp.json();
        if (res.success) {
            showToast(res.message);
            resetPromoForm();
            loadGlobalPromotions();
        } else {
            showToast(res.message || 'Error saving promotion', true);
        }
    } catch (e) { 
        console.error('Error:', e);
        showToast('Error saving promo', true); 
    }
}

async function loadGlobalPromotions() {
    try {
        const response = await fetch('/Admin/GetGlobalPromotions');
        const result = await response.json();
        const promotions = Array.isArray(result) ? result : (result.data || []);
        window.promotionsData = promotions;

        const list = document.getElementById('activePromoList');
        if (!list) return;

        if (promotions.length === 0) {
            list.innerHTML = '<div class="text-center py-5"><i class="bi bi-calendar2-x text-muted fs-1"></i><p class="text-muted small mt-2">No active promotions</p></div>';
            return;
        }

        list.innerHTML = promotions.map(promo => `
            <div class="bg-light p-3 rounded-4 border mb-2 d-flex align-items-center">
                <img src="${promo.bannerImageBase64 || '/images/challenge-placeholder.jpg'}" class="rounded-3 me-3" style="width: 50px; height: 50px; object-fit: cover;">
                <div class="flex-grow-1">
                    <div class="fw-bold small">${escapeHtml(promo.name)} (-${promo.discountPercent}%)</div>
                    <div class="small text-muted">${escapeHtml(promo.description || '')}</div>
                    <div class="tiny text-muted">Start: ${new Date(promo.startDate).toLocaleDateString()}</div>
                    ${promo.isIndefinite ? '<span class="badge bg-info small">Permanent</span>' : ''}
                </div>
                <div class="d-flex gap-1">
                    <button class="btn btn-sm btn-white border rounded-pill edit-promo-btn" data-promo-id="${promo.id}">Edit</button>
                    <button class="btn btn-sm btn-outline-danger rounded-pill delete-promo-btn" data-promo-id="${promo.id}">End</button>
                </div>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error loading promotions:', error);
        showToast('Error loading promotions', true);
    }
}

// Edit promotion function
function editPromo(promo) {
    currentEditingPromoId = promo.id;
    
    // Fill form fields
    document.getElementById('promoTitle').value = promo.name;
    document.getElementById('promoDesc').value = promo.description || '';
    document.getElementById('promoPercent').value = promo.discountPercent;
    document.getElementById('promoStart').value = promo.startDate.split('T')[0];
    
    const noEndDateCheckbox = document.getElementById('noEndDate');
    const promoEndInput = document.getElementById('promoEnd');
    
    if (promo.isIndefinite) {
        if (noEndDateCheckbox) noEndDateCheckbox.checked = true;
        if (promoEndInput) {
            promoEndInput.disabled = true;
            promoEndInput.value = '';
            promoEndInput.removeAttribute('required');
        }
    } else {
        if (noEndDateCheckbox) noEndDateCheckbox.checked = false;
        if (promoEndInput) {
            promoEndInput.disabled = false;
            promoEndInput.value = promo.endDate ? promo.endDate.split('T')[0] : '';
            promoEndInput.setAttribute('required', 'required');
        }
    }
    
    // Set image preview if exists
    if (promo.bannerImageBase64) {
        const preview = document.getElementById('promoPreview');
        const container = document.getElementById('promoPreviewContainer');
        if (preview && container) {
            preview.src = promo.bannerImageBase64;
            preview.setAttribute('data-base64', promo.bannerImageBase64);
            container.style.display = 'block';
            imageBase64Data = promo.bannerImageBase64;
        }
    }
    
    // Update form title
    const formTitle = document.getElementById('adminPromoFormTitle');
    if (formTitle) formTitle.innerText = 'Edit Global Promotion';
    
    // Scroll to form
    document.getElementById('admin-discounts')?.scrollIntoView({ behavior: 'smooth' });
}

// Delete promotion function
async function deletePromo(promoId) {
    if (!confirm('Are you sure you want to end this promotion? This action cannot be undone.')) return;
    
    showToast('Ending promotion...');
    
    try {
        const response = await fetch('/Admin/DeleteGlobalPromotion', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ id: promoId })
        });
        const result = await response.json();
        
        if (result.success) {
            showToast(result.message);
            loadGlobalPromotions();
        } else {
            showToast(result.message || 'Error ending promotion', true);
        }
    } catch (error) {
        console.error('Error:', error);
        showToast('Error ending promotion', true);
    }
}

// HELPER FUNCTIONS - Updated to match challengeDetails.js style
function showToast(message, isError = false) {
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');
    const toastEl = document.getElementById('financeToast');
    
    if (!toastMsg || !toastEl) {
        console.log('Toast element not found:', message);
        if (isError) alert('Error: ' + message);
        return;
    }
    
    toastMsg.innerText = message;
    toastIcon.className = isError ? 'bi bi-exclamation-triangle-fill text-danger fs-5' : 'bi bi-check-circle-fill text-success fs-5';
    
    // Change toast background based on error/success
    if (isError) {
        toastEl.classList.add('bg-danger', 'text-white');
        setTimeout(() => {
            toastEl.classList.remove('bg-danger', 'text-white');
        }, 3000);
    } else {
        toastEl.classList.add('bg-success', 'text-white');
        setTimeout(() => {
            toastEl.classList.remove('bg-success', 'text-white');
        }, 3000);
    }
    
    try {
        const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
        toast.show();
    } catch (err) {
        console.error('Error showing toast:', err);
    }
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function resetPromoForm() {
    currentEditingPromoId = null;
    imageBase64Data = null;
    bannerImageName = null;
    bannerImageContentType = null;
    
    // Reset form fields
    const form = document.getElementById('adminDiscountForm');
    if (form) form.reset();
    
    // Reset checkbox and end date field
    const noEndDateCheckbox = document.getElementById('noEndDate');
    const promoEndInput = document.getElementById('promoEnd');
    
    if (noEndDateCheckbox) {
        noEndDateCheckbox.checked = false;
    }
    
    if (promoEndInput) {
        promoEndInput.disabled = false;
        promoEndInput.classList.remove('is-invalid');
        promoEndInput.setAttribute('required', 'required');
    }
    
    // Reset image preview
    const promoPreview = document.getElementById('promoPreview');
    const promoPreviewContainer = document.getElementById('promoPreviewContainer');
    if (promoPreview) {
        promoPreview.src = '';
        promoPreview.removeAttribute('data-base64');
    }
    if (promoPreviewContainer) {
        promoPreviewContainer.style.display = 'none';
    }
    
    // Reset file input
    const fileInput = document.getElementById('promoImage');
    if (fileInput) {
        fileInput.value = '';
    }
    
    // Update form title
    const formTitle = document.getElementById('adminPromoFormTitle');
    if (formTitle) formTitle.innerText = 'Launch Global Promotion';
}