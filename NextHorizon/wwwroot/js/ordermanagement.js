let activeStatusFilter = 'all';

// Explicitly attach to 'window' so HTML onclick/oninput can ALWAYS find it
window.applyOrderFilters = function() {
    const searchInput = document.getElementById('orderSearch');
    const searchValue = searchInput ? searchInput.value.toLowerCase().trim() : '';

    const categorySelect = document.getElementById('categoryFilter');
    const categoryValue = categorySelect ? categorySelect.value : 'all';

    let currentStatus = 'all';
    if (typeof activeStatusFilter !== 'undefined') {
        currentStatus = activeStatusFilter.toLowerCase().trim();
    }

    const rows = document.querySelectorAll('.order-row');
    
    rows.forEach(row => {
        const status = (row.dataset.status || '').toLowerCase().trim();
        const rowText = row.innerText.toLowerCase();
        const rowCategories = (row.dataset.categories || '')
            .split('|')
            .map(category => category.toLowerCase().trim())
            .filter(Boolean);

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all'
            || rowCategories.includes(categoryValue.toLowerCase().trim());
        const searchMatch = !searchValue || rowText.includes(searchValue);

        if (statusMatch && categoryMatch && searchMatch) {
            row.style.display = ''; 
        } else {
            row.style.display = 'none'; 
        }
    });
};
function applyOrderFilters() {
    const searchInput = document.getElementById('orderSearch');
    const searchValue = searchInput ? searchInput.value.toLowerCase().trim() : '';

    const categorySelect = document.getElementById('categoryFilter');
    const categoryValue = categorySelect ? categorySelect.value : 'all';

    let currentStatus = 'all';
    if (typeof activeStatusFilter !== 'undefined') {
        currentStatus = activeStatusFilter.toLowerCase().trim();
    }

    const rows = document.querySelectorAll('.order-row');
    
    rows.forEach(row => {
        const status = (row.dataset.status || '').toLowerCase().trim();
        const rowText = row.innerText.toLowerCase();
        const rowCategories = (row.dataset.categories || '')
            .split('|')
            .map(category => category.toLowerCase().trim())
            .filter(Boolean);

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all'
            || rowCategories.includes(categoryValue.toLowerCase().trim());
        const searchMatch = !searchValue || rowText.includes(searchValue);

        if (statusMatch && categoryMatch && searchMatch) {
            row.style.display = ''; 
        } else {
            row.style.display = 'none'; 
        }
    });
}let selectedNoteOrderId = '';
let selectedNoteCustomer = '';
let selectedReviewOrderId = '';
let selectedShipmentOrderId = '';
let selectedReturnOrderId = '';
let selectedReturnOrderRow = null;


// ================= UTILS =================
function applyOrderFilters() {
    // 1. Get the search text
    const searchInput = document.getElementById('orderSearch');
    const searchValue = searchInput ? searchInput.value.toLowerCase().trim() : '';

    // 2. Get the category (if you have one)
    const categorySelect = document.getElementById('categoryFilter');
    const categoryValue = categorySelect ? categorySelect.value : 'all';

    // 3. Get the active status button
    let currentStatus = 'all';
    if (typeof activeStatusFilter !== 'undefined') {
        currentStatus = activeStatusFilter.toLowerCase().trim();
    }

    // 4. Filter the rows!
    const rows = document.querySelectorAll('.order-row');
    
    rows.forEach(row => {
        const status = (row.dataset.status || '').toLowerCase().trim();
        const rowText = row.innerText.toLowerCase();
        const rowCategories = (row.dataset.categories || '')
            .split('|')
            .map(category => category.toLowerCase().trim())
            .filter(Boolean);

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all'
            || rowCategories.includes(categoryValue.toLowerCase().trim());
        const searchMatch = !searchValue || rowText.includes(searchValue);

        // Show if it matches all criteria, hide if it doesn't
        if (statusMatch && categoryMatch && searchMatch) {
            row.style.display = ''; 
        } else {
            row.style.display = 'none'; 
        }
    });
}

// 1. Opens the premium modal and sets the exact Order ID
function declineOrder() {
    let orderIdText = "";
    const el1 = document.getElementById("summaryOrderId");
    const el2 = document.getElementById("modalOrderId");
    const el3 = document.getElementById("modalOrderNumber");

    if (el1 && el1.innerText) orderIdText = el1.innerText;
    else if (el2 && el2.innerText) orderIdText = el2.innerText;
    else if (el3 && el3.innerText) orderIdText = el3.innerText;

    orderIdText = orderIdText.replace("Order", "").replace("ORD-", "").replace("#", "").trim();

    const declineSpan = document.getElementById("declineModalOrderId");
    if (declineSpan) {
        declineSpan.innerText = orderIdText;
    }
    
    const reasonSelect = document.getElementById("declineReasonSelect");
    if (reasonSelect) {
        reasonSelect.value = "";
        reasonSelect.style.borderColor = "#d1d5db";
    }
    const errorText = document.getElementById("declineReasonError");
    if(errorText) errorText.style.display = "none";
    
    const summaryModal = document.getElementById("orderSummaryModal");
    if (summaryModal) summaryModal.style.display = "none";
    
    document.getElementById("declineOrderModal").style.display = "flex";
}

// Listen for clicks on the Status filter buttons
document.querySelectorAll('.order-filter-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        activeStatusFilter = this.getAttribute('data-filter');
        document.querySelectorAll('.order-filter-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        applyOrderFilters();
    });
});

function toggleOrderMenu(button) {
    const wrap = button.closest('.action-menu-wrap');
    if (!wrap) return;

    const menu = wrap.querySelector('.action-menu');
    const isOpen = menu.classList.contains('open');

    document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    
    if (!isOpen) {
        menu.classList.add('open');
    }
}

function updateOrderRowStatus(orderId, statusKey, statusLabel) {
    const orderRow = document.querySelector(`.order-row[data-order-id="${orderId}"]`);
    if (!orderRow) return;

    orderRow.dataset.status = statusKey;

    const badge = orderRow.querySelector('.status-badge');
    if (badge) {
        badge.textContent = statusLabel;
        badge.className = `status-badge ${statusKey}`;
    }
}

// PHASE 4: MARK AS SHIPPED - Step 4: Manual Status Update & Tracking Input
function openMarkShippedModal(orderId, customerName, courierName) {
    const modal = document.getElementById('markShippedModal');
    if (!modal) return;

    document.getElementById('shipModalOrderId').innerText = orderId;
    document.getElementById('shipModalCustomer').innerText = customerName;
    document.getElementById('shipModalCourier').innerText = courierName || "NextHorizon Partner";
    
    const trackingInput = document.getElementById('shipTrackingNumber');
    if(trackingInput) {
        trackingInput.value = "";
        trackingInput.style.borderColor = "#d1d5db";
    }

    modal.style.display = 'flex';
    modal.style.zIndex = '99999';

    setTimeout(() => trackingInput?.focus(), 100);
}
async function submitShipment() {
    // 1. Get the elements
    const orderId = document.getElementById('shipModalOrderId').innerText;
    const trackingNumber = document.getElementById('shipTrackingNumber').value.trim();
    const fileInput = document.getElementById('shipReceiptImage');
    const submitBtn = document.querySelector('#markShippedModal .bw-btn-primary') || document.getElementById('confirmShipmentBtn');
    // 2. Validation
    if (!trackingNumber) {
        showToast("Tracking number is required.", "error");
        return;
    }

    // --- START LOADING STATE ---
    // Disable the button so they can't click it twice
    submitBtn.disabled = true;
    // Change the text and add a spinner icon
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Processing...';

    // 3. Prepare the Data
    const formData = new FormData();
    formData.append('OrderId', parseInt(orderId));
    formData.append('TrackingNumber', trackingNumber);
    if (fileInput.files.length > 0) {
        formData.append('ProofOfShipment', fileInput.files[0]);
    }

    try {
        const response = await fetch('/Dashboard/MarkOrderShipped', {
            method: 'POST',
            body: formData // No headers needed for FormData
        });

        const result = await response.json();

        if (result.success) {
            // 1. Target the modal elements to replace them with the animation
            const modalBody = document.querySelector('#markShippedModal .bw-modal-body');
            const modalFooter = document.querySelector('#markShippedModal .bw-modal-footer');
            
            // 2. Hide the footer (Cancel/Confirm buttons)
            if(modalFooter) modalFooter.style.display = 'none';

            // 3. Inject the Success Animation HTML into the body
            modalBody.innerHTML = `
                <div class="success-animation" style="padding: 40px 20px; display: flex; flex-direction: column; align-items: center;">
                    <div class="checkmark-wrapper" style="font-size: 70px; color: #10b981; animation: checkmark-pop 0.5s ease-out;">
                        <i class="fa-solid fa-circle-check"></i>
                    </div>
                    <h4 style="text-align: center; margin-top: 15px; color: #111827; margin-bottom: 5px;">Order Shipped!</h4>
                    <p style="text-align: center; font-size: 0.85rem; color: #6b7280;">Updating your dashboard...</p>
                </div>
            `;

            // 4. Reload after 2 seconds so they can see the checkmark
            setTimeout(() => {
                location.reload();
            }, 2000);

        } else {
            // Handle error case
            showToast(result.message, "error");
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnText;
        }
    } catch (error) {
        // Handle network error
        console.error("Error:", error);
        showToast("System error. Please try again.", "error");
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalBtnText;
    }
}
function closeMarkShippedModal() {
    document.getElementById('markShippedModal').style.display = 'none';
    removePreview(); // Reset the image preview
    document.getElementById('shipTrackingNumber').value = ""; // Clear the tracking input
}
function openTrackingRequiredModal() {
    document.getElementById('trackingRequiredModal').style.display = 'flex';
}

function closeTrackingRequiredModal() {
    document.getElementById('trackingRequiredModal').style.display = 'none';
}

function openReturnedInfoModal(orderRow) {
    if (!orderRow) return;

    selectedReturnOrderId = orderRow.dataset.orderId || '';
    document.getElementById('returnInfoOrderId').textContent = selectedReturnOrderId;
    document.getElementById('returnInfoCustomer').textContent = orderRow.children[1]?.innerText || '---';
    document.getElementById('returnInfoProduct').textContent = orderRow.children[3]?.innerText || '---';
    document.getElementById('returnInfoQuantity').textContent = orderRow.children[4]?.innerText || '0';
    document.getElementById('returnInfoTotal').textContent = orderRow.children[5]?.innerText || '\u20B10.00';
    document.getElementById('returnInfoCourier').textContent = orderRow.dataset.courier || 'J&T Express';
    document.getElementById('returnInfoTracking').textContent = orderRow.dataset.tracking || '123456789';
    document.getElementById('returnInfoProofImage').src = orderRow.dataset.returnProof || 'https://picsum.photos/seed/return-proof-1/360/220';
    document.getElementById('returnInfoNote').textContent = orderRow.dataset.returnNote || 'Package was sent back because the buyer could not be reached.';
    document.getElementById('returnedInfoModal').style.display = 'flex';
}

function closeReturnedInfoModal() {
    document.getElementById('returnedInfoModal').style.display = 'none';
}

function openConfirmReturnModal() {
    document.getElementById('confirmReturnOrderId').textContent = selectedReturnOrderId || '';
    document.getElementById('confirmReturnModal').style.display = 'flex';
}

function closeConfirmReturnModal() {
    document.getElementById('confirmReturnModal').style.display = 'none';
}

function openReviewRequestModal(orderId) {
    selectedReviewOrderId = orderId || '';
    document.getElementById('reviewRequestModal')?.classList.add('active');
}

function closeReviewRequestModal() {
    document.getElementById('reviewRequestModal')?.classList.remove('active');
}
// ================= MASTER CLICK EVENT LISTENER =================
document.addEventListener('click', function (event) {
    
    // View Details Button
    const viewDetailsBtn = event.target.closest('.view-details-btn');
    if (viewDetailsBtn) {
        event.preventDefault();
        event.stopPropagation();
        
        const orderId = viewDetailsBtn.getAttribute('data-order-id') || viewDetailsBtn.dataset.orderId;
        if (orderId) {
            openViewOrderModal(orderId);
        } else {
            console.warn("View Details button clicked but missing data-order-id attribute!");
        }
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        return;
    }

    // 1. Mark as Shipped Button (Input Tracking)
    const markAsShippedBtn = event.target.closest('.mark-as-shipped-btn');
    if (markAsShippedBtn) {
        event.preventDefault();
        event.stopPropagation();
        
        const orderId = markAsShippedBtn.getAttribute('data-order-id');
        const customer = markAsShippedBtn.getAttribute('data-customer');
        const courierName = markAsShippedBtn.getAttribute('data-courier-name'); 
        
        openMarkShippedModal(orderId, customer, courierName);
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        return;
    }

    // 2. Add Note Button
    const addNoteBtn = event.target.closest('.add-note-btn');
    if (addNoteBtn) {
        event.preventDefault();
        event.stopPropagation(); 
        
        const orderId = addNoteBtn.getAttribute('data-order-id') || addNoteBtn.dataset.orderId || "Unknown";
        const customer = addNoteBtn.getAttribute('data-customer') || addNoteBtn.dataset.customer || "Customer";
        
        openAddNoteModal(orderId, customer);
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        return;
    }

    // 3. Save Note Button (Direct save)
    if (event.target.id === 'addNotesSaveBtn') {
        event.preventDefault();
        saveOrderNote();
        return; 
    }

    // 4. Action menu toggle
    const actionBtn = event.target.closest('.action-icon-btn');
    if (actionBtn) {
        toggleOrderMenu(actionBtn);
        return;
    }

    // 5. Mark Returned Button
    const markReturnedBtn = event.target.closest('.mark-returned-btn');
    if (markReturnedBtn) {
        event.stopPropagation();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        openReturnedInfoModal(markReturnedBtn.closest('.order-row'));
        return;
    }

    // 6. Review request
    const reviewBtn = event.target.closest('.review-request-btn');
    if (reviewBtn) {
        openReviewRequestModal(reviewBtn.dataset.orderId);
        return;
    }

    // 7. Close menus ONLY if not clicking menu items
    if (!event.target.closest('.action-menu') && !event.target.classList.contains('action-icon-btn')) {
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    }

    // 8. Close modals on overlay
    if (event.target.id === 'addNotesModal') closeAddNotesModal();
    if (event.target.id === 'reviewRequestModal') closeReviewRequestModal();
    if (event.target.id === 'markShippedModal') closeMarkShippedModal();
    if (event.target.id === 'confirmShipmentModal') closeConfirmShipmentModal();
    if (event.target.id === 'trackingRequiredModal') closeTrackingRequiredModal();
    if (event.target.id === 'markReturnedModal') closeReturnModal();
    if (event.target.id === 'returnedInfoModal') closeReturnedInfoModal();
});

document.getElementById('addNotesCancelBtn')?.addEventListener('click', closeAddNotesModal);
document.getElementById('reviewRequestCancelBtn')?.addEventListener('click', closeReviewRequestModal);
document.getElementById('reviewRequestSubmitBtn')?.addEventListener('click', closeReviewRequestModal);

// Filter buttons
document.querySelectorAll('.order-filter-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        // Fallback added here to prevent undefined errors in older browsers
        activeStatusFilter = this.getAttribute('data-filter') || this.dataset.filter;
        document.querySelectorAll('.order-filter-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        applyOrderFilters();
    });
});

// Search/inputs
document.getElementById('orderSearch')?.addEventListener('input', applyOrderFilters);
document.getElementById('categoryFilter')?.addEventListener('change', applyOrderFilters);
document.getElementById('searchBtn')?.addEventListener('click', applyOrderFilters);
document.getElementById('clearDate')?.addEventListener('click', function() {
    document.getElementById('startDate').value = '';
    document.getElementById('endDate').value = '';
    applyOrderFilters();
});
document.getElementById('returnSearch')?.addEventListener('input', applyOrderFilters);
document.getElementById('returnStatusFilter')?.addEventListener('change', applyOrderFilters);
document.getElementById('returnSearchBtn')?.addEventListener('click', applyOrderFilters);
document.getElementById('returnStartDate')?.addEventListener('change', applyOrderFilters);
document.getElementById('returnEndDate')?.addEventListener('change', applyOrderFilters);
document.getElementById('clearReturnDate')?.addEventListener('click', function() {
    document.getElementById('returnStartDate').value = '';
    document.getElementById('returnEndDate').value = '';
    applyOrderFilters();
});

// ESC key
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeAddNotesModal();
        closeReviewRequestModal();
        closeMarkShippedModal();
        closeTrackingRequiredModal();
        closeReturnModal();
        closeReturnedInfoModal();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    }
});
    
    
    // ================= INIT & EVENT BINDING =================
document.addEventListener('DOMContentLoaded', function () {
    
    // 1. Bind the Filter Tabs
    document.querySelectorAll('.order-filter-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            activeStatusFilter = this.getAttribute('data-filter') || this.dataset.filter;
            document.querySelectorAll('.order-filter-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            applyOrderFilters();
        });
    });


    // 2. Bind the Search Bar and Category Dropdown
    document.getElementById('orderSearch')?.addEventListener('input', applyOrderFilters);
    document.getElementById('categoryFilter')?.addEventListener('change', applyOrderFilters);
    document.getElementById('searchBtn')?.addEventListener('click', applyOrderFilters);
    
    document.getElementById('clearDate')?.addEventListener('click', function() {
        document.getElementById('startDate').value = '';
        document.getElementById('endDate').value = '';
        applyOrderFilters();
    });
    document.getElementById('returnSearch')?.addEventListener('input', applyOrderFilters);
    document.getElementById('returnStatusFilter')?.addEventListener('change', applyOrderFilters);
    document.getElementById('returnSearchBtn')?.addEventListener('click', applyOrderFilters);
    document.getElementById('returnStartDate')?.addEventListener('change', applyOrderFilters);
    document.getElementById('returnEndDate')?.addEventListener('change', applyOrderFilters);
    document.getElementById('clearReturnDate')?.addEventListener('click', function() {
        document.getElementById('returnStartDate').value = '';
        document.getElementById('returnEndDate').value = '';
        applyOrderFilters();
    });

    // 3. Apply filters immediately on load just in case
    applyOrderFilters();
});


// ===== ORDER SUMMARY =====
function openOrderSummaryModal(orderId) {
    document.getElementById('summaryOrderId').textContent = orderId;
    document.getElementById('orderSummaryModal').classList.add('active');
}

function closeOrderSummaryModal() {
    const modal = document.getElementById("orderSummaryModal");
    if (modal) {
        modal.style.display = "none";
    }
}

function confirmOrder() {
    closeOrderSummaryModal();
    const modal = document.getElementById('processingModal');
    if(modal) modal.classList.add('active');

    setTimeout(() => {
        if(modal) modal.classList.remove('active');
        updateOrderRowStatus(selectedReviewOrderId, 'to-ship', 'TO SHIP');
        applyOrderFilters();
        openToShipModal(selectedReviewOrderId);
    }, 2500);
}

// ===== ORDER TO SHIP MODAL =====
function openToShipModal(orderId) {
    const modal = document.getElementById('orderToShipModal');
    const message = document.getElementById('toShipMessage');
    if(message) message.textContent = `Order #${orderId} has been moved to TO SHIP.`;
    modal?.classList.add('active');
}

function closeToShipModal() {
    document.getElementById('orderToShipModal')?.classList.remove('active');
}

document.getElementById('toShipOkBtn')?.addEventListener('click', closeToShipModal);

// ===== DOWNLOAD RECEIPT MODAL =====
function openDownloadReceiptModal(order) {
    const orderDate = order?.DateTime ? new Date(order.DateTime) : null;
    const totalAmount = Number(order?.TotalAmount || 0);
    const quantity = Number(order?.Quantity || 0);
    const unitPrice = quantity > 0 ? totalAmount / quantity : totalAmount;

    document.getElementById('receiptOrderId').textContent = order?.OrderId || '';
    document.getElementById('receiptCustomer').textContent = order?.Customer || '';
    document.getElementById('receiptDate').textContent =
        orderDate && !Number.isNaN(orderDate.getTime())
            ? orderDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
            : '';
    document.getElementById('receiptTotal').textContent = totalAmount.toFixed(2);

    const tbody = document.getElementById('receiptItemsBody');
    tbody.innerHTML = ''; 
    order.Items = Array.isArray(order?.Items) && order.Items.length
        ? order.Items
        : [{ ProductName: order?.ProductName || '', Quantity: quantity, Price: unitPrice }];

    order.Items.forEach(item => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
            <td>${item.ProductName}</td>
            <td>x${item.Quantity}</td>
            <td>₱${item.Price.toFixed(2)}</td>
            <td>₱${(item.Price * item.Quantity).toFixed(2)}</td>
        `;
        tbody.appendChild(tr);
    });

    document.getElementById('downloadReceiptModal').classList.add('active');
}

function closeDownloadReceiptModal() {
    document.getElementById('downloadReceiptModal').classList.remove('active');
}

function downloadReceipt() {
    alert('Receipt download started! (implement backend PDF)');
    closeDownloadReceiptModal();
}

document.getElementById('downloadReceiptModal')?.addEventListener('click', function(e) {
    if(e.target.id === 'downloadReceiptModal') closeDownloadReceiptModal();
});

let currentReviewOrder = null;

function submitReviewRequest() {
    if (!currentReviewOrder) return;

    const decision = document.querySelector('input[name="reviewDecision"]:checked')?.value;
    const comment = document.getElementById('reviewComment').value.trim();

    if (!decision) {
        alert("Please select Approve or Reject.");
        return;
    }

    console.log("Order", currentReviewOrder.OrderId, "Decision:", decision, "Comment:", comment);
    closeReviewRequestModal();
    alert(`Review submitted for order #${currentReviewOrder.OrderId}.`);
}

function confirmShipment() {
    const tracking = document.getElementById("trackingNumber")?.value.trim();

    if (!tracking) {
        openTrackingRequiredModal();
        return;
    }
}

function closeReturnModal() {
    closeReturnedInfoModal();
}

function confirmReturn() {
    completeReturn();
}

function completeReturn() {
    updateOrderRowStatus(selectedReturnOrderId, 'failed-delivery', 'Failed Delivery');
    closeReturnedInfoModal();
    openConfirmReturnModal();
    applyOrderFilters();
}

function showToast(message, type = 'success') {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    
    const toast = document.createElement('div');
    toast.className = `premium-toast ${type}`;
    
    const icon = type === 'success' 
        ? `<svg class="toast-icon" style="width: 24px; height: 24px;" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`
        : `<svg class="toast-icon" style="width: 24px; height: 24px;" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>`;

    toast.innerHTML = `${icon} <span>${message}</span>`;
    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}

// --- PHASE 1: ACCEPT ORDER FLOW ---
async function confirmAndAcceptOrder() {
    const summaryOrderIdElement = document.getElementById("summaryOrderId");
    if (!summaryOrderIdElement) {
        showToast("Unable to find order ID. Please refresh and try again.", "error");
        return;
    }

    const orderIdText = summaryOrderIdElement.innerText;
    const orderId = Number.parseInt(orderIdText.replace("ORD-", "").trim(), 10);
    if (Number.isNaN(orderId)) {
        showToast("Invalid order ID. Cannot confirm order.", "error");
        return;
    }

    const courierDropdown = document.getElementById("courierSelect");
    const selectedCourierId = courierDropdown?.value;
    const acceptButton = document.querySelector('#orderSummaryModal button[onclick="confirmAndAcceptOrder()"]');
    const originalButtonHtml = acceptButton ? acceptButton.innerHTML : "";

    if (!selectedCourierId) {
        document.getElementById("courierWarning").style.display = "flex";
        courierDropdown?.classList.add("input-error");
        return;
    }

    if (acceptButton) {
        acceptButton.disabled = true;
        acceptButton.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Accepting...';
    }

    try {
        const response = await fetch('/Dashboard/AcceptOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                OrderId: orderId,
                Courier: parseInt(selectedCourierId, 10)
            })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(`HTTP ${response.status}: ${text}`);
        }

        const result = await response.json();

        if (result.success) {
            showToast(result.message || `Order #${orderId} accepted successfully.`, 'success');
            setTimeout(() => {
                location.reload();
            }, 1200);
            return;
        }

        showToast(result.message || 'Unable to accept order.', 'error');
    } catch (error) {
        console.error('Server error:', error);
        showToast('A network error occurred. Please try again.', 'error');
    } finally {
        if (acceptButton) {
            acceptButton.disabled = false;
            acceptButton.innerHTML = originalButtonHtml;
        }
    }
}

function hideCourierWarning() {
    const warningEl = document.getElementById("courierWarning");
    const selectEl = document.getElementById("courierSelect");
    
    if (warningEl) warningEl.style.display = "none";
    if (selectEl) selectEl.classList.remove("input-error");
}

async function openViewOrderModal(orderId) {
    if (!orderId) return;

    // Strip non-numeric characters so the backend receives a valid integer
    const cleanOrderId = String(orderId).replace(/\D/g, '');
    if (!cleanOrderId) return;

    console.log("Opening modal for order:", cleanOrderId);
    let modal = document.getElementById("orderSummaryModal");
    
    if (!modal) {
        console.error("ERROR: Modal not found in any way!");
        return;
    }
    
    modal.style.display = "flex";
    
    try {
        const response = await fetch(`/Dashboard/GetOrderDetails?orderId=${cleanOrderId}`);
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
        const result = await response.json();

        if (result.success) {
            const order = result.data;

            const realId = order.orderID || order.orderId || order.OrderID;
            const el = document.getElementById("summaryOrderId");
            if (el) el.innerText = realId;
            
            const rawDate = order.orderDate || order.OrderDate;
            if (rawDate) {
                const dateEl = document.getElementById("modalOrderDate");
                if (dateEl) dateEl.innerText = new Date(rawDate).toLocaleDateString();
            }

            const payEl = document.getElementById("modalPayment");
            if (payEl) payEl.innerText = order.paymentMethod || order.PaymentMethod || "N/A";
            
            const total = order.totalAmount || order.TotalAmount || 0;
            const totEl = document.getElementById("modalTotal");
            if (totEl) totEl.innerText = total.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });
            
            const custEl = document.getElementById("modalCustomerName");
            if (custEl) custEl.innerText = order.fullName || order.FullName || "Unknown";
            
            const street = order.streetAddress || order.StreetAddress || '';
            const city = order.city || order.City || '';
            const postal = order.postalCode || order.PostalCode || '';
            const fullAddress = `${street}, ${city} ${postal}`.trim();
            const addrEl = document.getElementById("modalCustomerAddress");
            if (addrEl) addrEl.innerText = fullAddress || "No address provided";

            const phoneEl = document.getElementById("modalCustomerPhone");
            if (phoneEl) phoneEl.innerText = order.phoneNumber || order.PhoneNumber || "No phone number";

            const emailEl = document.getElementById("modalCustomerEmail");
            if (emailEl) emailEl.innerText = order.email || order.Email || "No email";

            const delOptionEl = document.getElementById("modalDeliveryOption");
            if (delOptionEl) delOptionEl.innerText = order.deliveryOption || order.DeliveryOption || "Standard";

            const qty = order.quantity || order.Quantity || 0;
            const subtotal = order.calculatedSubtotal || order.CalculatedSubtotal || order.subtotal || 0;
            const shipping = order.shippingFee || order.ShippingFee || 0;
            const grandTotal = order.calculatedTotal || order.CalculatedTotal || order.totalAmount || 0;
            const totItemsEl = document.getElementById("modalTotalItems");
            if (totItemsEl) totItemsEl.innerText = qty;

            const subtotalEl = document.getElementById("modalSubtotal");
            if (subtotalEl) subtotalEl.innerText = subtotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });

            const shipEl = document.getElementById("modalShippingFee");
            if (shipEl) shipEl.innerText = shipping.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });

            const grandEl = document.getElementById("modalGrandTotal");
            if (grandEl) grandEl.innerText = grandTotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });
            const topTotalEl = document.getElementById("modalTotal");
            if (topTotalEl) {
                topTotalEl.innerText = grandTotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });
            }
          
           const itemsBodyEl = document.getElementById("modalItemsBody");

            if (itemsBodyEl) {
                itemsBodyEl.innerHTML = "";
                const itemsList = order.orderItems || order.OrderItems;

                if (itemsList && itemsList.length > 0) {
                    itemsList.forEach(item => {
                        const itemName = (item.product && item.product.name) || (item.Product && item.Product.name) || (order.productName) || (order.ProductName) || "Unknown Item";
                        const color = item.color || item.Color || "";
                        const size = item.size || item.Size || "";
                        let variant = `${color} ${size}`.trim();
                        if (!variant) variant = "Standard Variant"; 

                        const itemQty = item.quantity || item.Quantity || 1;
                        const itemPrice = item.unitPrice || item.UnitPrice || 0;
                        const lineTotal = itemQty * itemPrice;

                        itemsBodyEl.innerHTML += `
                            <tr>
                                <td>
                                    <strong style="display: block; color: #111827;">${itemName}</strong>
                                    <span style="font-size: 0.8rem; color: #6b7280;">${variant}</span>
                                </td>
                                <td style="text-align: center; font-weight: 500;">x${itemQty}</td>
                                <td style="text-align: right; font-weight: 500;">${lineTotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' })}</td>
                            </tr>
                        `;
                    });
                } else {
                    const fallbackName = order.productName || order.ProductName || "Product Name Unavailable";
                    const fallbackQty = order.quantity || order.Quantity || 0;
                    const fallbackTotal = order.subtotal || order.Subtotal || 0;
                    
                    const colors = order.colors || order.Colors;
                    const fallbackVariant = colors ? `Color: ${colors}` : "Standard Variant";

                    itemsBodyEl.innerHTML = `
                        <tr>
                            <td>
                                <strong style="display: block; color: #111827;">${fallbackName}</strong>
                                <span style="font-size: 0.8rem; color: #6b7280;">${fallbackVariant}</span>
                            </td>
                            <td style="text-align: center; font-weight: 500;">x${fallbackQty}</td>
                            <td style="text-align: right; font-weight: 500;">${fallbackTotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' })}</td>
                        </tr>
                    `;
                }
            }
        } else {
            console.error("API Error:", result.message);
            alert("Could not load order details: " + result.message);
            if (modal) modal.style.display = "none";
        }
    } catch (error) {
        console.error("Error fetching order:", error);
        alert("Error loading order. Check console for details.");
        if (modal) modal.style.display = "none";
    }
}

function copyCustomerAddress() {
    const addressElement = document.getElementById("modalCustomerAddress");
    
    if (!addressElement || addressElement.innerText.trim() === "" || addressElement.innerText.includes("Loading")) {
        showToast("No address available to copy.", "error");
        return;
    }

    const addressText = addressElement.innerText.trim();
    navigator.clipboard.writeText(addressText).then(() => {
        showToast("Address copied to clipboard!", "success");
    }).catch(err => {
        console.error('Could not copy text: ', err);
        showToast("Failed to copy address. Check browser permissions.", "error");
    });
}

function openDeclineModal(orderId) {
    document.getElementById('declineOrderId').value = orderId;
    document.getElementById('declineOrderNumberDisplay').innerText = orderId;
    
    document.querySelector('input[name="declineReason"][value="Out of Stock"]').checked = true;
    toggleDeclineTextarea();
    document.getElementById('customDeclineReason').value = "";
    
    document.getElementById('declineOrderModal').style.display = 'flex';
}

function closeDeclineModal() {
    document.getElementById('declineOrderModal').style.display = 'none';
}

function toggleDeclineTextarea() {
    const selected = document.querySelector('input[name="declineReason"]:checked').value;
    const customContainer = document.getElementById('customReasonContainer');
    
    if (selected === "Other") {
        customContainer.style.display = 'block';
    } else {
        customContainer.style.display = 'none';
    }
}

async function submitDeclineOrder() {
    // 1. Get Order ID safely from the hidden input
    const orderIdVal = document.getElementById("declineOrderId").value;
    const orderId = parseInt(orderIdVal); 
    
    if (isNaN(orderId) || orderId === 0) {
        showToast("System Error: Could not detect the Order ID.", "error");
        return; 
    }

    // 2. Read the selected Radio Button
    const selectedRadio = document.querySelector('input[name="declineReason"]:checked');
    if (!selectedRadio) {
        showToast("Please select a reason.", "error");
        return;
    }

    let reason = selectedRadio.value;

    // 3. Handle the "Other" custom text area
    if (reason === "Other") {
        reason = document.getElementById("customDeclineReason").value.trim();
        if (!reason) {
            document.getElementById("customDeclineReason").style.borderColor = "#ef4444";
            showToast("Please specify your reason.", "error");
            return;
        }
    }

    // 4. Send to Backend
    try {
        const response = await fetch('/Dashboard/DeclineOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ OrderId: orderId, Reason: reason })
        });

        const result = await response.json();

        if (response.ok || result.success) {
            closeDeclineModal();
            showToast(`Order #${orderId} was declined successfully.`, "success");
            setTimeout(() => location.reload(), 1500); 
        } else {
            showToast(result.message || "Failed to decline order.", "error");
        }
    } catch (error) {
        console.error("Server error:", error);
        showToast("A network error occurred.", "error");
    }
}

function openAddNoteModal(orderId, customerName) {
    const modal = document.getElementById('addNotesModal');
    if (!modal) {
        console.error("Modal not found in HTML!");
        return; 
    }

    const idInput = document.getElementById('addNotesOrderId');
    const headerText = document.getElementById('customerHeaderText');
    const textarea = document.getElementById('addNotesTextarea');

    if (idInput) idInput.value = orderId;
    if (headerText) headerText.innerText = `Add Note for Order #${orderId}`; 
    if (textarea) textarea.value = ""; 

    modal.style.display = 'flex';
    modal.style.zIndex = '99999'; 
}

function openAddNotesModal(orderId, customerName) {
    openAddNoteModal(orderId, customerName);
}

function closeAddNotesModal() {
    const modal = document.getElementById('addNotesModal');
    if (modal) modal.style.display = 'none';
}

async function saveOrderNote() {
    const orderId = document.getElementById('addNotesOrderId').value;
    const noteText = document.getElementById('addNotesTextarea').value;

    if(!noteText.trim()) {
        showToast("Please enter a note before saving.", "error"); 
        return;
    }

    try {
        const response = await fetch(`/Dashboard/SaveOrderNote`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ orderId: parseInt(orderId), note: noteText })
        });

        if (response.ok) {
            closeAddNotesModal();
            showToast("Note saved successfully!", "success"); 
            
            const orderRow = document.querySelector(`.order-row[data-order-id="${orderId}"]`);
            if (orderRow) {
                const customerCell = orderRow.children[1]; 
                
                if (customerCell && !customerCell.innerHTML.includes('fa-note-sticky')) {
                    customerCell.innerHTML += ` <span title="Has Notes" style="color: #f59e0b; margin-left: 8px;"><i class="fa-solid fa-note-sticky"></i></span>`;
                }
            }
        } else {
            showToast("Error saving note. Please try again.", "error"); 
        }
    } catch (error) {
        console.error("Error:", error);
        showToast("System error while saving.", "error");
    }
}
// --- Global function for saving notes on the Order Details page ---
async function savePageNote(orderId) {
    const noteInput = document.getElementById('pageNoteInput');
    
    if (!noteInput) {
        console.error("Could not find the note input field.");
        return;
    }

    const noteText = noteInput.value;

    try {
        const response = await fetch('/Dashboard/SaveOrderNote', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ OrderId: parseInt(orderId), Note: noteText })
        });

        const result = await response.json();

        if (result.success === true) {
            showToast(result.message, "success"); 
            setTimeout(() => location.reload(), 1500); 
        } else {
            showToast("Error: " + result.message, "error"); 
        }
    } catch (error) {
        console.error("Error:", error);
        showToast("System error while saving.", "error");
    }
}

function previewImage(input) {
    const container = document.getElementById('receiptPreviewContainer');
    const preview = document.getElementById('receiptPreview');

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            preview.src = e.target.result;
            container.style.display = 'block';
        }
        reader.readAsDataURL(input.files[0]);
    }
}

function removePreview() {
    const fileInput = document.getElementById('shipReceiptImage');
    const container = document.getElementById('receiptPreviewContainer');
    fileInput.value = ""; // Clears the file selection
    container.style.display = 'none'; // Hides the preview box
}



document.getElementById('returnUploadBox')?.addEventListener('click', function () {
    document.getElementById('returnProof')?.click();
});

document.getElementById('returnProof')?.addEventListener('change', function () {
    previewReturnProof(this);
});

function openMarkReturnedModal(orderRow) {
    if (!orderRow) return;

    selectedReturnOrderRow = orderRow;
    selectedReturnOrderId = orderRow.dataset.orderId || '';
    document.getElementById('returnOrderId').textContent = selectedReturnOrderId;
    document.getElementById('returnCustomer').textContent = orderRow.children[1]?.innerText.trim() || '---';
    document.getElementById('returnProduct').textContent = orderRow.children[3]?.innerText.trim() || '---';
    document.getElementById('returnItems').textContent = orderRow.children[4]?.innerText.trim() || '0';
    document.getElementById('returnTotal').textContent = orderRow.children[5]?.innerText.trim() || '\u20B10.00';
    document.getElementById('returnCourier').textContent = orderRow.dataset.courier || '---';
    document.getElementById('returnTracking').textContent = orderRow.dataset.tracking || '---';
    document.getElementById('returnReason').value = '';
    document.getElementById('returnNote').value = '';
    removeReturnPreview();
    document.getElementById('markReturnedModal').style.display = 'flex';
}

function closeReturnModal() {
    document.getElementById('markReturnedModal').style.display = 'none';
    selectedReturnOrderRow = null;
    removeReturnPreview();
}

function openReturnedInfoModal(orderRow) {
    if (!orderRow) return;

    selectedReturnOrderId = orderRow.dataset.orderId || '';
    document.getElementById('returnInfoOrderId').textContent = selectedReturnOrderId;
    document.getElementById('returnInfoCustomer').textContent = orderRow.children[1]?.innerText.trim() || '---';
    document.getElementById('returnInfoProduct').textContent = orderRow.children[3]?.innerText.trim() || '---';
    document.getElementById('returnInfoQuantity').textContent = orderRow.children[4]?.innerText.trim() || '0';
    document.getElementById('returnInfoTotal').textContent = orderRow.children[5]?.innerText.trim() || '\u20B10.00';
    document.getElementById('returnInfoCourier').textContent = orderRow.dataset.courier || '---';
    document.getElementById('returnInfoTracking').textContent = orderRow.dataset.tracking || '---';
    document.getElementById('returnInfoReason').textContent = orderRow.dataset.returnReason || '---';
    document.getElementById('returnInfoNote').textContent = orderRow.dataset.returnNote || 'No notes provided.';

    const proofUrl = orderRow.dataset.returnProof || '';
    const proofBlock = document.getElementById('returnInfoProofBlock');
    const proofImage = document.getElementById('returnInfoProofImage');
    if (proofUrl) {
        proofImage.src = proofUrl;
        proofBlock.style.display = 'block';
    } else {
        proofImage.removeAttribute('src');
        proofBlock.style.display = 'none';
    }

    document.getElementById('returnedInfoModal').style.display = 'flex';
}

function closeReturnedInfoModal() {
    document.getElementById('returnedInfoModal').style.display = 'none';
}

function previewReturnProof(input) {
    const container = document.getElementById('returnPreviewContainer');
    const preview = document.getElementById('returnPreviewImage');
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (event) {
            preview.src = event.target.result;
            container.style.display = 'block';
        };
        reader.readAsDataURL(input.files[0]);
    }
}

function removeReturnPreview() {
    const fileInput = document.getElementById('returnProof');
    const container = document.getElementById('returnPreviewContainer');
    const preview = document.getElementById('returnPreviewImage');
    if (fileInput) fileInput.value = '';
    if (preview) preview.removeAttribute('src');
    if (container) container.style.display = 'none';
}

async function submitReturnedOrder() {
    const orderId = parseInt(selectedReturnOrderId, 10);
    const reason = document.getElementById('returnReason').value.trim();
    const note = document.getElementById('returnNote').value.trim();
    const fileInput = document.getElementById('returnProof');
    const submitBtn = document.getElementById('confirmReturnBtn');

    if (!orderId || !reason) {
        showToast('Return reason is required.', 'error');
        return;
    }

    const formData = new FormData();
    formData.append('OrderId', orderId);
    formData.append('ReturnReason', reason);
    formData.append('ReturnNote', note);
    if (fileInput && fileInput.files.length > 0) {
        formData.append('ReturnProof', fileInput.files[0]);
    }

    const originalText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin"></i> Processing...';

    try {
        const response = await fetch('/Dashboard/MarkOrderReturned', {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            setTimeout(() => location.reload(), 1200);
            return;
        }

        showToast(result.message || 'Unable to mark order as returned.', 'error');
    } catch (error) {
        console.error(error);
        showToast('System error while processing return.', 'error');
    } finally {
        submitBtn.disabled = false;
        submitBtn.innerHTML = originalText;
    }
}

document.addEventListener('click', function (event) {
    const markReturnedBtn = event.target.closest('.mark-returned-btn');
    if (markReturnedBtn) {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        openMarkReturnedModal(markReturnedBtn.closest('.order-row'));
        return;
    }

    const viewReturnedBtn = event.target.closest('.view-returned-btn');
    if (viewReturnedBtn) {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        openReturnedInfoModal(viewReturnedBtn.closest('.order-row'));
    }
}, true);

document.getElementById('returnUploadBox')?.addEventListener('click', function () {
    document.getElementById('returnProof')?.click();
});

document.getElementById('returnProof')?.addEventListener('change', function () {
    previewReturnProof(this);
});



function parseOrderManagementDate(value, inclusiveEnd = false) {
    if (!value) {
        return null;
    }

    const parsedDate = new Date(`${value}T00:00:00`);
    if (Number.isNaN(parsedDate.getTime())) {
        return null;
    }

    if (inclusiveEnd) {
        parsedDate.setHours(23, 59, 59, 999);
    }

    return parsedDate;
}

function updateOrderManagementEmptyState(visibleRowCount) {
    const noOrdersRow = document.getElementById('noOrdersRow');
    if (!noOrdersRow) {
        return;
    }

    noOrdersRow.style.display = visibleRowCount === 0 ? '' : 'none';
}

function updateReturnManagementEmptyState(visibleRowCount) {
    const noReturnsRow = document.getElementById('noReturnsRow');
    if (!noReturnsRow) {
        return;
    }

    noReturnsRow.style.display = visibleRowCount === 0 ? '' : 'none';
}
window.applyOrderFilters = function () {
    const searchInput = document.getElementById('orderSearch');
    const searchValue = searchInput ? searchInput.value.toLowerCase().trim() : '';

    const categorySelect = document.getElementById('categoryFilter');
    const categoryValue = categorySelect ? categorySelect.value.toLowerCase().trim() : 'all';

    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');
    const startDateValue = startDateInput ? startDateInput.value : '';
    const endDateValue = endDateInput ? endDateInput.value : '';
    const startDate = parseOrderManagementDate(startDateValue);
    const endDate = parseOrderManagementDate(endDateValue, true);

    let currentStatus = 'all';
    if (typeof activeStatusFilter !== 'undefined') {
        currentStatus = activeStatusFilter.toLowerCase().trim();
    }

    let visibleRowCount = 0;
    const rows = document.querySelectorAll('.order-row');

    rows.forEach(row => {
        const status = (row.dataset.status || '').toLowerCase().trim();
        const rowText = row.innerText.toLowerCase();
        const rowCategories = (row.dataset.categories || '')
            .split('|')
            .map(category => category.toLowerCase().trim())
            .filter(Boolean);
        const rowDate = parseOrderManagementDate(row.dataset.orderDate || '');

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all' || rowCategories.includes(categoryValue);
        const searchMatch = !searchValue || rowText.includes(searchValue);
        const startDateMatch = !startDate || (rowDate && rowDate >= startDate);
        const endDateMatch = !endDate || (rowDate && rowDate <= endDate);

        if (statusMatch && categoryMatch && searchMatch && startDateMatch && endDateMatch) {
            row.style.display = '';
            visibleRowCount += 1;
        } else {
            row.style.display = 'none';
        }
    });

    updateOrderManagementEmptyState(visibleRowCount);
};

applyOrderFilters = window.applyOrderFilters;

function buildOrderManagementDateFilterUrl() {
    const url = new URL(window.location.href);
    const startDateValue = document.getElementById('startDate')?.value?.trim() || '';
    const endDateValue = document.getElementById('endDate')?.value?.trim() || '';

    if (startDateValue) {
        url.searchParams.set('startDate', startDateValue);
    } else {
        url.searchParams.delete('startDate');
    }

    if (endDateValue) {
        url.searchParams.set('endDate', endDateValue);
    } else {
        url.searchParams.delete('endDate');
    }

    return url.toString();
}

function submitOrderManagementDateFilter() {
    window.location.assign(buildOrderManagementDateFilterUrl());
}

function clearOrderManagementDateFilter() {
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');

    if (startDateInput) {
        startDateInput.value = '';
    }

    if (endDateInput) {
        endDateInput.value = '';
    }

    const url = new URL(window.location.href);
    url.searchParams.delete('startDate');
    url.searchParams.delete('endDate');
    window.location.assign(url.toString());
}

function initializeOrderManagementStatusFromUrl() {
    const url = new URL(window.location.href);
    const requestedStatus = (url.searchParams.get('status') || '').trim().toLowerCase();
    if (!requestedStatus) {
        return;
    }

    const matchingButton = Array.from(document.querySelectorAll('.order-filter-btn')).find(function (button) {
        return (button.getAttribute('data-filter') || '').trim().toLowerCase() === requestedStatus;
    });

    if (!matchingButton) {
        return;
    }

    activeStatusFilter = matchingButton.getAttribute('data-filter') || matchingButton.dataset.filter || 'all';
    document.querySelectorAll('.order-filter-btn').forEach(function (button) {
        button.classList.remove('active');
    });
    matchingButton.classList.add('active');
}
function initializeOrderManagementDateFilter() {
    document.getElementById('startDate')?.addEventListener('change', submitOrderManagementDateFilter);
    document.getElementById('endDate')?.addEventListener('change', submitOrderManagementDateFilter);
    document.getElementById('clearDate')?.addEventListener('click', function (event) {
        event.preventDefault();
        clearOrderManagementDateFilter();
    });

    initializeOrderManagementStatusFromUrl();
    window.applyOrderFilters();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeOrderManagementDateFilter);
} else {
    initializeOrderManagementDateFilter();
}



let selectedReturnRequestRow = null;

function openReturnDetailsModalFromRow(row) {
    if (!row) {
        return;
    }

    selectedReturnRequestRow = row;
    const returnId = row.dataset.returnId || '';
    const orderId = row.dataset.orderId || '';
    const buyer = row.dataset.buyer || 'Buyer';
    const reason = row.dataset.reason || 'Not provided';
    const message = row.dataset.message || 'No additional message provided.';
    const imageUrl = row.dataset.imageUrl || '';
    const status = row.dataset.returnStatus || row.dataset.displayStatus || row.dataset.status || 'Return Requested';
    const sellerDecisionReason = row.dataset.sellerDecisionReason || '';
    const sellerDecisionNote = row.dataset.sellerDecisionNote || '';
    const reviewedAt = row.dataset.reviewedAt || ''; 

    document.getElementById('returnDetailsOrderId').textContent = orderId;
    document.getElementById('returnDetailsBuyer').textContent = buyer;
    document.getElementById('returnDetailsReason').textContent = reason;
    document.getElementById('returnDetailsMessage').textContent = message;

    const statusBadge = document.getElementById('returnDetailsStatus');
    statusBadge.textContent = status;
    statusBadge.className = 'status-badge ' + status.toLowerCase().replace(/\s+/g, '-');

    const imageElement = document.getElementById('returnDetailsImage');
    imageElement.src = imageUrl || 'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" width="720" height="250"%3E%3Crect width="100%25" height="100%25" rx="18" fill="%23f8fafc"/%3E%3Ctext x="50%25" y="50%25" dominant-baseline="middle" text-anchor="middle" fill="%2364758b" font-family="Arial" font-size="20"%3ENo image uploaded%3C/text%3E%3C/svg%3E';
    imageElement.alt = 'Return evidence for order #' + orderId;

    const refundWrap = document.getElementById('refundStockWrap');
    const restoreStockCheckbox = document.getElementById('restoreStockCheckbox');
    restoreStockCheckbox.checked = false;
    refundWrap.style.display = status === 'Item Returned' ? '' : 'none';

    renderReturnSellerReview(status, sellerDecisionReason, sellerDecisionNote, reviewedAt);
    renderReturnDetailsActions(status, returnId, orderId);
    document.getElementById('returnDetailsModal').style.display = 'flex';
}

function renderReturnSellerReview(status, sellerDecisionReason, sellerDecisionNote, reviewedAt) {
    const sellerReviewCard = document.getElementById('sellerReviewCard');
    const decisionReasonElement = document.getElementById('returnDecisionReason');
    const decisionNoteElement = document.getElementById('returnDecisionNote');
    const reviewedAtElement = document.getElementById('returnReviewedAt');

    if (!sellerReviewCard || !decisionReasonElement || !decisionNoteElement || !reviewedAtElement) {
        return;
    }

    const shouldShow = status === 'Return Rejected' && (!!sellerDecisionReason || !!sellerDecisionNote || !!reviewedAt);
    sellerReviewCard.style.display = shouldShow ? '' : 'none';
    decisionReasonElement.textContent = sellerDecisionReason || 'No reason provided.';
    decisionNoteElement.textContent = sellerDecisionNote || 'No additional note provided.';
    reviewedAtElement.textContent = reviewedAt || 'Not recorded.';
}

function renderReturnDetailsActions(status, returnId, orderId) {
    const actions = document.getElementById('returnDetailsActions');
    actions.innerHTML = '';

    const parsedReturnId = parseInt(returnId, 10);
    const hasReturnId = Number.isFinite(parsedReturnId) && parsedReturnId > 0;

    if (status === 'Return Requested' && hasReturnId) {
        actions.appendChild(buildReturnActionButton('Reject', 'return-btn return-btn-secondary', function () {
            openRejectReturnModal(parsedReturnId, orderId);
        }));
        actions.appendChild(buildReturnActionButton('Approve', 'return-btn return-btn-primary', function () {
            updateReturnRequestStatus('/Dashboard/ReviewReturnRequest', { returnId: parsedReturnId, decision: 'approve' });
        }));
        return;
    }

    if (status === 'Return Approved' && hasReturnId) {
        actions.appendChild(buildReturnActionButton('Mark as Item Returned', 'return-btn return-btn-primary', function () {
            updateReturnRequestStatus('/Dashboard/MarkReturnItemReceived', { returnId: parsedReturnId });
        }));
        return;
    }

    if (status === 'Item Returned' && hasReturnId) {
        actions.appendChild(buildReturnActionButton('Confirm Refund', 'return-btn return-btn-primary', function () {
            updateReturnRequestStatus('/Dashboard/ConfirmReturnRefund', {
                returnId: parsedReturnId,
                restoreStock: document.getElementById('restoreStockCheckbox').checked
            });
        }));
        return;
    }

    if (status === 'Return Rejected') {
        actions.appendChild(buildReturnStatusNote('This return request has been rejected. No further action is available.'));
        return;
    }

    if (status === 'Refunded') {
        actions.appendChild(buildReturnStatusNote('This return request has already been refunded and completed.'));
        return;
    }

    if (hasReturnId) {
        actions.appendChild(buildReturnStatusNote('This return request is already up to date.'));
    }
}

function closeReturnDetailsModal() {
    document.getElementById('returnDetailsModal').style.display = 'none';
    selectedReturnRequestRow = null;
}

function openRejectReturnModal(returnId, orderId) {
    document.getElementById('rejectReturnId').value = returnId || '';
    document.getElementById('rejectReturnReason').value = '';
    document.getElementById('rejectReturnNote').value = '';
    document.getElementById('rejectReturnReasonError').style.display = 'none';
    document.getElementById('returnDetailsModal').style.display = 'none';
    document.getElementById('rejectReturnModal').classList.add('active');
}

function closeRejectReturnModal() {
    document.getElementById('rejectReturnModal').classList.remove('active');
    if (selectedReturnRequestRow) {
        document.getElementById('returnDetailsModal').style.display = 'flex';
    }
}

async function submitRejectReturnRequest() {
    const returnId = parseInt(document.getElementById('rejectReturnId').value, 10);
    const rejectionReason = (document.getElementById('rejectReturnReason').value || '').trim();
    const rejectionNote = (document.getElementById('rejectReturnNote').value || '').trim();
    const errorElement = document.getElementById('rejectReturnReasonError');

    if (!rejectionReason) {
        errorElement.style.display = 'block';
        return;
    }

    errorElement.style.display = 'none';
    await updateReturnRequestStatus('/Dashboard/ReviewReturnRequest', {
        returnId: returnId,
        decision: 'reject',
        rejectionReason: rejectionReason,
        rejectionNote: rejectionNote
    });
}

function buildReturnStatusNote(message) {
    const note = document.createElement('div');
    note.className = 'return-action-note';
    note.textContent = message;
    return note;
}

function buildReturnActionButton(label, className, onClick) {
    const button = document.createElement('button');
    button.type = 'button';
    button.className = className;
    button.textContent = label;
    button.addEventListener('click', onClick);
    return button;
}

async function updateReturnRequestStatus(url, payload) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const result = await response.json();
        if (!result.success) {
            showToast(result.message || 'Unable to update return request.', 'error');
            return;
        }

        showToast(result.message || 'Return request updated.', 'success');
        setTimeout(function () {
            location.reload();
        }, 800);
    } catch (error) {
        console.error(error);
        showToast('System error while updating the return request.', 'error');
    }
}

function toggleOrderManagementPanels(currentStatus) {
    const ordersPanel = document.getElementById('ordersPanel');
    const returnsPanel = document.getElementById('returnsPanel');
    const isReturnView = currentStatus === 'return';

    if (ordersPanel) {
        ordersPanel.style.display = isReturnView ? 'none' : '';
    }

    if (returnsPanel) {
        returnsPanel.style.display = isReturnView ? '' : 'none';
    }
}

window.applyOrderFilters = function () {
    const searchInput = document.getElementById('orderSearch');
    const searchValue = searchInput ? searchInput.value.toLowerCase().trim() : '';

    const categorySelect = document.getElementById('categoryFilter');
    const categoryValue = categorySelect ? categorySelect.value.toLowerCase().trim() : 'all';

    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');
    const startDateValue = startDateInput ? startDateInput.value : '';
    const endDateValue = endDateInput ? endDateInput.value : '';
    const startDate = parseOrderManagementDate(startDateValue);
    const endDate = parseOrderManagementDate(endDateValue, true);

    let currentStatus = 'all';
    if (typeof activeStatusFilter !== 'undefined') {
        currentStatus = activeStatusFilter.toLowerCase().trim();
    }

    toggleOrderManagementPanels(currentStatus);

    if (currentStatus === 'return') {
        const returnSearchInput = document.getElementById('returnSearch');
        const returnSearchValue = returnSearchInput ? returnSearchInput.value.toLowerCase().trim() : '';
        const returnStatusSelect = document.getElementById('returnStatusFilter');
        const returnStatusValue = returnStatusSelect ? returnStatusSelect.value.toLowerCase().trim() : 'all';
        const returnStartDateInput = document.getElementById('returnStartDate');
        const returnEndDateInput = document.getElementById('returnEndDate');
        const returnStartDate = parseOrderManagementDate(returnStartDateInput ? returnStartDateInput.value : '');
        const returnEndDate = parseOrderManagementDate(returnEndDateInput ? returnEndDateInput.value : '', true);
        const returnRows = document.querySelectorAll('.return-row');
        let visibleReturnRows = 0;

        returnRows.forEach(function (row) {
            const rowText = row.innerText.toLowerCase();
            const rowDate = parseOrderManagementDate(row.dataset.date || '');
            const rowStatus = (row.dataset.status || '').toLowerCase().trim();
            const searchMatch = !returnSearchValue || rowText.includes(returnSearchValue);
            const statusMatch = returnStatusValue === 'all' || rowStatus === returnStatusValue;
            const startDateMatch = !returnStartDate || (rowDate && rowDate >= returnStartDate);
            const endDateMatch = !returnEndDate || (rowDate && rowDate <= returnEndDate);

            if (searchMatch && statusMatch && startDateMatch && endDateMatch) {
                row.style.display = '';
                visibleReturnRows += 1;
            } else {
                row.style.display = 'none';
            }
        });

        updateReturnManagementEmptyState(visibleReturnRows);
        return;
    }

    let visibleRowCount = 0;
    const rows = document.querySelectorAll('.order-row');

    rows.forEach(function (row) {
        const status = (row.dataset.status || '').toLowerCase().trim();
        const rowText = row.innerText.toLowerCase();
        const rowCategories = (row.dataset.categories || '')
            .split('|')
            .map(function (category) { return category.toLowerCase().trim(); })
            .filter(Boolean);
        const rowDate = parseOrderManagementDate(row.dataset.orderDate || '');

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all' || rowCategories.includes(categoryValue);
        const searchMatch = !searchValue || rowText.includes(searchValue);
        const startDateMatch = !startDate || (rowDate && rowDate >= startDate);
        const endDateMatch = !endDate || (rowDate && rowDate <= endDate);

        if (statusMatch && categoryMatch && searchMatch && startDateMatch && endDateMatch) {
            row.style.display = '';
            visibleRowCount += 1;
        } else {
            row.style.display = 'none';
        }
    });

    updateOrderManagementEmptyState(visibleRowCount);
};

applyOrderFilters = window.applyOrderFilters;

document.addEventListener('click', function (event) {
    const returnDetailsBtn = event.target.closest('.return-view-details-btn');
    if (returnDetailsBtn) {
        event.preventDefault();
        const sourceRow = returnDetailsBtn.closest('.return-row') || returnDetailsBtn.closest('.order-row');
        openReturnDetailsModalFromRow(sourceRow);
        return;
    }

    if (event.target.id === 'returnDetailsModal') {
        closeReturnDetailsModal();
    }

    if (event.target.id === 'rejectReturnModal') {
        closeRejectReturnModal();
    }
});

document.getElementById('returnDetailsImage')?.addEventListener('click', function () {
    if (this.src) {
        window.open(this.src, '_blank');
    }
});

