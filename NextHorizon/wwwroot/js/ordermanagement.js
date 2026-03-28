let activeStatusFilter = 'all';
let selectedNoteOrderId = '';
let selectedNoteCustomer = '';
let selectedReviewOrderId = '';
let selectedShipmentOrderId = '';
let selectedReturnOrderId = '';

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

        const statusMatch = currentStatus === 'all' || status === currentStatus;
        const categoryMatch = categoryValue === 'all';
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

// Step 5: System Execution & Backend Update
async function submitShipment() {
    const orderId = document.getElementById('shipModalOrderId').innerText;
    const trackingNumber = document.getElementById('shipTrackingNumber').value.trim();

    if (!trackingNumber) {
        showToast("Tracking number is required to confirm shipment.", "error");
        document.getElementById('shipTrackingNumber').style.borderColor = "#ef4444";
        return;
    }

    try {
        const response = await fetch('/Dashboard/MarkOrderShipped', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ 
                orderId: parseInt(orderId), 
                trackingNumber: trackingNumber 
            })
        });

        const result = await response.json();

        if (result.success) {
            closeMarkShippedModal();
            showToast(`Order #${orderId} marked as Shipped!`, "success");
            setTimeout(() => location.reload(), 1500);
        } else {
            showToast(result.message, "error");
        }
    } catch (error) {
        console.error("Error:", error);
        showToast("System error. Please try again.", "error");
    }
}

function closeMarkShippedModal() {
    document.getElementById('markShippedModal').style.display = 'none';
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
    document.getElementById('returnInfoTotal').textContent = orderRow.children[5]?.innerText || '0.00';
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
    
    // 1. Mark as Shipped Button
    const markAsShippedBtn = event.target.closest('.mark-as-shipped-btn');
    if (markAsShippedBtn) {
        event.preventDefault();
        event.stopPropagation();
        
        console.log("BUTTON CLICKED! Catching data...");
        
        const orderId = markAsShippedBtn.getAttribute('data-order-id');
        const customer = markAsShippedBtn.getAttribute('data-customer');
        const courierId = markAsShippedBtn.getAttribute('data-courier-id'); 
        const courierName = markAsShippedBtn.getAttribute('data-courier-name'); 
        
        console.log("Order:", orderId, "Customer:", customer, "Courier:", courierName);
        
        openMarkShippedModal(orderId, customer, courierName);
        
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        return;
    }

    // 2. Add Note Button
    const addNoteBtn = event.target.closest('.add-note-btn');
    if (addNoteBtn) {
        event.preventDefault();
        event.stopPropagation(); 
        
        const orderId = addNoteBtn.dataset.orderId || addNoteBtn.getAttribute('data-order-id') || "Unknown";
        const customer = addNoteBtn.dataset.customer || addNoteBtn.getAttribute('data-customer') || "Customer";
        
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
    if (event.target.id === 'returnedInfoModal') closeReturnedInfoModal();
    if (event.target.id === 'confirmReturnModal') closeConfirmReturnModal();
});

document.getElementById('addNotesCancelBtn')?.addEventListener('click', closeAddNotesModal);
document.getElementById('reviewRequestCancelBtn')?.addEventListener('click', closeReviewRequestModal);
document.getElementById('reviewRequestSubmitBtn')?.addEventListener('click', closeReviewRequestModal);

// Filter buttons
document.querySelectorAll('.order-filter-btn').forEach(btn => {
    btn.addEventListener('click', function () {
        activeStatusFilter = this.dataset.filter;
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

// ESC key
document.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') {
        closeAddNotesModal();
        closeReviewRequestModal();
        closeMarkShippedModal();
        closeTrackingRequiredModal();
        closeReturnedInfoModal();
        closeConfirmReturnModal();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    }
});

// ================= INIT =================
document.addEventListener('DOMContentLoaded', function () {
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
    updateOrderRowStatus(selectedReturnOrderId, 'return', 'Return');
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
        alert("Unable to find order ID. Please refresh and try again.");
        return;
    }

    const orderIdText = summaryOrderIdElement.innerText;
    const orderId = Number.parseInt(orderIdText.replace("ORD-", "").trim(), 10);
    if (Number.isNaN(orderId)) {
        alert("Invalid order ID. Cannot confirm order.");
        return;
    }

    const courierDropdown = document.getElementById("courierSelect"); 
    const selectedCourierId = courierDropdown.value;

    if (!selectedCourierId) {
        document.getElementById("courierWarning").style.display = "flex";
        courierDropdown.classList.add("input-error");
        return; 
    }

    try {
        const response = await fetch('/Dashboard/AcceptOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                OrderId: orderId,
                Courier: parseInt(selectedCourierId)
            })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(`HTTP ${response.status}: ${text}`);
        }

        const result = await response.json();

       if (result.success) {
             showToast(`Order #${orderId} was added successfully.`, "success");
            setTimeout(() => {
                location.reload();
            }, 1500);

        } else {
            showToast(result.message, "error");
        }
    } catch (error) {
        console.error("Server error:", error);
        showToast("A network error occurred. Please try again.", "error");
    }
}

function hideCourierWarning() {
    const warningEl = document.getElementById("courierWarning");
    const selectEl = document.getElementById("courierSelect");
    
    if (warningEl) warningEl.style.display = "none";
    if (selectEl) selectEl.classList.remove("input-error");
}

async function openViewOrderModal(orderId) {
    console.log("Opening modal for order:", orderId);
    let modal = document.getElementById("orderSummaryModal");
    
    if (!modal) {
        console.error("ERROR: Modal not found in any way!");
        return;
    }
    
    modal.classList.add('active');
    
    try {
        const response = await fetch(`/Dashboard/GetOrderDetails?orderId=${orderId}`);
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
            if (modal) modal.style.display = "flex";
        } else {
            console.error("API Error:", result.message);
            alert("Could not load order details: " + result.message);
        }
    } catch (error) {
        console.error("Error fetching order:", error);
        alert("Error loading order. Check console for details.");
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