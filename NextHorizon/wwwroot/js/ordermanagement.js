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
    // 1. The Bloodhound: Check all possible IDs your background modal might be using
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
        // Update the active status variable
        activeStatusFilter = this.getAttribute('data-filter');
        
        // Move the visual 'active' styling to the clicked button
        document.querySelectorAll('.order-filter-btn').forEach(b => b.classList.remove('active'));
        this.classList.add('active');
        
        // Run the engine!
        applyOrderFilters();
    });
});

// 3. Validates and sends the data to the server
function toggleOrderMenu(button) {
    const wrap = button.closest('.action-menu-wrap');
    if (!wrap) return;

    const menu = wrap.querySelector('.action-menu');
    const isOpen = menu.classList.contains('open');

    // Close all others
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

    // Populate modal with order context
    document.getElementById('shipModalOrderId').innerText = orderId;
    document.getElementById('shipModalCustomer').innerText = customerName;
    document.getElementById('shipModalCourier').innerText = courierName || "NextHorizon Partner";
    
    // Clear previous input
    const trackingInput = document.getElementById('shipTrackingNumber');
    if(trackingInput) {
        trackingInput.value = "";
        trackingInput.style.borderColor = "#d1d5db";
    }

    modal.style.display = 'flex';
    modal.style.zIndex = '99999';

    // Auto-focus for scanner efficiency
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
            
            // Reload to move the order from 'To Ship' tab to 'Shipped' tab
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

// ================= EVENTS =================
document.addEventListener('click', function (event) {
    
    // Action menu toggle
    const actionBtn = event.target.closest('.action-icon-btn');
    if (actionBtn) {
        toggleOrderMenu(actionBtn);
        return;
    }

    const markAsShippedBtn = event.target.closest('.mark-as-shipped-btn');
    if (markAsShippedBtn) {
        event.stopPropagation();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        openMarkShippedModal(markAsShippedBtn.closest('.order-row'));
        return;
    }

    const markReturnedBtn = event.target.closest('.mark-returned-btn');
    if (markReturnedBtn) {
        event.stopPropagation();
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
        openReturnedInfoModal(markReturnedBtn.closest('.order-row'));
        return;
    }

    // Review request
    const reviewBtn = event.target.closest('.review-request-btn');
    if (reviewBtn) {
        openReviewRequestModal(reviewBtn.dataset.orderId);
        return;
    }

    // Close menus ONLY if not clicking menu items
    if (!event.target.closest('.action-menu') && !event.target.classList.contains('action-icon-btn')) {
        document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    }

    // Close modals on overlay
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
        closeNoteSavedModal();
        closeMarkShippedModal();
        closeConfirmShipmentModal();
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
    // Hide summary
    closeOrderSummaryModal();

    // Show processing
    const modal = document.getElementById('processingModal');
    modal.classList.add('active');

    // Simulate backend processing
    setTimeout(() => {
        // Hide processing
        modal.classList.remove('active');

        // Update the order row status
        updateOrderRowStatus(selectedReviewOrderId, 'to-ship', 'TO SHIP');

        // Re-apply filters so the "To Ship" filter sees it
        applyOrderFilters();

        // Open the modal
        openToShipModal(selectedReviewOrderId);

    }, 2500);
}

// ===== ORDER TO SHIP MODAL =====
function openToShipModal(orderId) {
    const modal = document.getElementById('orderToShipModal');
    const message = document.getElementById('toShipMessage');
    message.textContent = `Order #${orderId} has been moved to TO SHIP.`;
    modal?.classList.add('active');
}

function closeToShipModal() {
    document.getElementById('orderToShipModal')?.classList.remove('active');
}

// OK button
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
    tbody.innerHTML = ''; // clear previous items
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

// Example download function (you can replace with actual backend/pdf generation)
function downloadReceipt() {
    alert('Receipt download started! (implement backend PDF)');
    closeDownloadReceiptModal();
}

document.getElementById('downloadReceiptModal').addEventListener('click', function(e) {
    if(e.target.id === 'downloadReceiptModal') closeDownloadReceiptModal();
});

let currentReviewOrder = null;

// Open modal and populate info
function openReviewRequestModal(order) {
    currentReviewOrder = order;

    document.getElementById('reviewOrderId').textContent = order.OrderId;
    document.getElementById('reviewCustomer').textContent = order.Customer;
    document.getElementById('reviewProduct').textContent = order.ProductName;
    document.getElementById('reviewReason').textContent = order.ReturnReason;

    const photoContainer = document.getElementById('reviewPhotos');
    photoContainer.innerHTML = '';

    if (order.Attachments && order.Attachments.length > 0) {
        order.Attachments.forEach(url => {
            const img = document.createElement('img');
            img.src = url;
            img.style.width = '80px';
            img.style.height = '80px';
            img.style.objectFit = 'cover';
            img.style.marginRight = '10px';
            img.style.borderRadius = '4px';
            photoContainer.appendChild(img);
        });
    } else {
        photoContainer.innerHTML = '<p>No attachments provided.</p>';
    }

    // Clear previous selection
    const radios = document.getElementsByName('reviewDecision');
    radios.forEach(r => r.checked = false);
    document.getElementById('reviewComment').value = '';

    document.getElementById('reviewRequestModal').classList.add('active');
}

// Close modal
function closeReviewRequestModal() {
    document.getElementById('reviewRequestModal').classList.remove('active');
    currentReviewOrder = null;
}

// Submit decision
function submitReviewRequest() {
    if (!currentReviewOrder) return;

    const decision = document.querySelector('input[name="reviewDecision"]:checked')?.value;
    const comment = document.getElementById('reviewComment').value.trim();

    if (!decision) {
        alert("Please select Approve or Reject.");
        return;
    }

    // TODO: send decision + comment to backend via fetch/ajax
    console.log("Order", currentReviewOrder.OrderId, "Decision:", decision, "Comment:", comment);

    // Close modal
    closeReviewRequestModal();

    alert(`Review submitted for order #${currentReviewOrder.OrderId}.`);
}

// Shipment modal logic is handled through the delegated click listener above.

// CONFIRM SHIPMENT (Step 5)
function confirmShipment() {
    const tracking = document.getElementById("trackingNumber").value.trim();

    if (!tracking) {
        openTrackingRequiredModal();
        return;
    }

    // 🔥 SEND TO BACKEND (AJAX)
    openConfirmShipmentModal();
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
    const toast = document.createElement('div');
    toast.className = `premium-toast ${type}`;
    
    // Choose checkmark for success, X for error
    const icon = type === 'success' 
        ? `<svg class="toast-icon" style="width: 24px; height: 24px;" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`
        : `<svg class="toast-icon" style="width: 24px; height: 24px;" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>`;

    toast.innerHTML = `${icon} <span>${message}</span>`;
    container.appendChild(toast);

    // Make it disappear after 3.5 seconds
    setTimeout(() => {
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 300);
    }, 3500);
}
// --- PHASE 1: ACCEPT ORDER FLOW ---
async function confirmAndAcceptOrder() {
    // 1. Grab the Order ID and selected Courier from the modal
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

    // ✨ NEW UI VALIDATION: Stop them if they didn't pick a courier
    if (!selectedCourierId) {
        // Show the sleek red warning instead of the old browser alert
        document.getElementById("courierWarning").style.display = "flex";
        courierDropdown.classList.add("input-error");
        return; 
    }

    // 2. Send the data to your C# Controller
    try {
        const response = await fetch('/Dashboard/AcceptOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                OrderId: orderId,
                Courier: selectedCourierId // Sending the ID from the dropdown
            })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(`HTTP ${response.status}: ${text}`);
        }

        const result = await response.json();

       if (result.success) {
            // Show the premium toast!
             showToast(`Order #${orderId} was added successfully.`, "success");
            
            // Wait 1.5 seconds to let the user read it, THEN reload the page
            setTimeout(() => {
                location.reload();
            }, 1500);

        } else {
            // Show the red error toast
            showToast(result.message, "error");
        }
    } catch (error) {
        console.error("Server error:", error);
        // Show the red error toast for network crashes
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
    
    // Debug: List ALL elements with class containing 'modal'
    const allModals = document.querySelectorAll('[class*="modal"]');
    console.log("Found " + allModals.length + " elements with 'modal' in class");
    allModals.forEach(el => console.log("  -", el.id, el.className));
    
    // Try different selector variations
    let modal = document.getElementById("orderSummaryModal");
    console.log("getElementById('orderSummaryModal'):", modal);
    
    if (!modal) {
        modal = document.querySelector("[id='orderSummaryModal']");
        console.log("querySelector with [id='orderSummaryModal']:", modal);
    }
    
    if (!modal) {
        console.error("ERROR: Modal not found in any way!");
        return;
    }
    
    console.log("Modal found! Adding active class...");
    modal.classList.add('active');
    
    // Now try to fetch order details
    try {
        const response = await fetch(`/Dashboard/GetOrderDetails?orderId=${orderId}`);
        const result = await response.json();

        if (result.success) {
            const order = result.data;
            console.log("ORDER DATA:", order); 

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
            // 1. Combine Address Fields
            const street = order.streetAddress || order.StreetAddress || '';
            const city = order.city || order.City || '';
            const postal = order.postalCode || order.PostalCode || '';
            const fullAddress = `${street}, ${city} ${postal}`.trim();
            const addrEl = document.getElementById("modalCustomerAddress");
            if (addrEl) addrEl.innerText = fullAddress || "No address provided";

            // 2. Customer Contact Info
            const phoneEl = document.getElementById("modalCustomerPhone");
            if (phoneEl) phoneEl.innerText = order.phoneNumber || order.PhoneNumber || "No phone number";

            const emailEl = document.getElementById("modalCustomerEmail");
            if (emailEl) emailEl.innerText = order.email || order.Email || "No email";

            const delOptionEl = document.getElementById("modalDeliveryOption");
            if (delOptionEl) delOptionEl.innerText = order.deliveryOption || order.DeliveryOption || "Standard";

            // 3. Financial Breakdown
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
                    
                    //  SCENARIO A: WE HAVE MULTIPLE ITEMS - Loop through them!
                    itemsList.forEach(item => {
                        // 1. Dig into the linked Product table to get the name
                        const itemName = (item.product && item.product.name) || (item.Product && item.Product.name) || (order.productName) || (order.ProductName) || "Unknown Item";
                        
                        // 2. Combine Color and Size from the OrderItem table
                        const color = item.color || item.Color || "";
                        const size = item.size || item.Size || "";
                        let variant = `${color} ${size}`.trim();
                        if (!variant) variant = "Standard Variant"; // Fallback if no color/size exists

                        // 3. Calculate Math
                        const itemQty = item.quantity || item.Quantity || 1;
                        const itemPrice = item.unitPrice || item.UnitPrice || 0;
                        const lineTotal = itemQty * itemPrice;

                        // 4. Build the HTML Row
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
                    
                    //  SCENARIO B: FALLBACK FOR OLD ORDERS
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
            console.log("Modal data populated successfully");

            const modalEl = document.getElementById("orderSummaryModal");
            if (modalEl) modalEl.style.display = "flex";
        } else {
            console.error("API Error:", result.message);
            alert("Could not load order details: " + result.message);
        }
    } catch (error) {
        console.error("Error fetching order:", error);
        alert("Error loading order. Check console for details.");
    }
}
// Function to close the View Order modal

// 1. Open the Modal (Wire your "Decline" button to this!)
function openDeclineModal(orderId) {
    document.getElementById('declineOrderId').value = orderId;
    document.getElementById('declineOrderNumberDisplay').innerText = orderId;
    
    // Reset the form
    document.querySelector('input[name="declineReason"][value="Out of Stock"]').checked = true;
    toggleDeclineTextarea();
    document.getElementById('customDeclineReason').value = "";
    
    document.getElementById('declineOrderModal').style.display = 'flex';
}

function closeDeclineModal() {
    document.getElementById('declineOrderModal').style.display = 'none';
}

// 2. Toggle the custom text box
function toggleDeclineTextarea() {
    const selected = document.querySelector('input[name="declineReason"]:checked').value;
    const customContainer = document.getElementById('customReasonContainer');
    
    if (selected === "Other") {
        customContainer.style.display = 'block';
    } else {
        customContainer.style.display = 'none';
    }
}

// 3. Submit to Backend
async function submitDeclineOrder() {
    const summaryIdElement = document.getElementById("summaryOrderId");
    const orderIdText = summaryIdElement ? summaryIdElement.innerText : ""; 

    const orderId = parseInt(orderIdText.replace("ORD-", "").replace("#", "").trim()); 
    
    if (isNaN(orderId) || orderId === 0) {
        showToast("System Error: Could not detect the Order ID.", "error");
        return; 
    }

    const reasonSelect = document.getElementById("declineReasonSelect");
    const reason = reasonSelect.value;
    const errorText = document.getElementById("declineReasonError");

    if (!reason || reason === "") {
        reasonSelect.style.borderColor = "#ef4444"; 
        if(errorText) errorText.style.display = "block"; 
        return; 
    }

    reasonSelect.style.borderColor = "#d1d5db";
    if(errorText) errorText.style.display = "none";

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

    // Inject data for context
    const idInput = document.getElementById('addNotesOrderId');
    const headerText = document.getElementById('customerHeaderText');
    const textarea = document.getElementById('addNotesTextarea');

    if (idInput) idInput.value = orderId;
    if (headerText) headerText.innerText = `Add Note for Order #${orderId}`; // Phase 3 exact text
    if (textarea) textarea.value = ""; 

    // Force UI Presentation
    modal.style.display = 'flex';
    modal.style.zIndex = '99999'; 
}

// Bridge to catch any old HTML button clicks
function openAddNotesModal(orderId, customerName) {
    openAddNoteModal(orderId, customerName);
}

function closeAddNotesModal() {
    const modal = document.getElementById('addNotesModal');
    if (modal) modal.style.display = 'none';
}

// STEP 3, 4 & 5: DATA ENTRY, EXECUTION, AND UI FEEDBACK
async function saveOrderNote() {
    const orderId = document.getElementById('addNotesOrderId').value;
    const noteText = document.getElementById('addNotesTextarea').value;

    if(!noteText.trim()) {
        showToast("Please enter a note before saving.", "error"); 
        return;
    }

    try {
        // Step 4: System Execution
        const response = await fetch(`/Dashboard/SaveOrderNote`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ orderId: parseInt(orderId), note: noteText })
        });

        if (response.ok) {
            // Step 5: UI Feedback & Visibility
            closeAddNotesModal();
            showToast("Note saved successfully!", "success"); 
            
            // ✨ THE CRUCIAL UI UPDATE: Add a sticky note icon to the order row!
            const orderRow = document.querySelector(`.order-row[data-order-id="${orderId}"]`);
            if (orderRow) {
                // Assuming customer name is the 2nd column (index 1). Adjust if needed!
                const customerCell = orderRow.children[1]; 
                
                // Only add the icon if it doesn't already have one
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

// THE VIP EVENT LISTENER (Catches the click cleanly)
document.addEventListener('click', function(event) {
const shipBtn = event.target.closest('.mark-as-shipped-btn');
if (shipBtn) {
    event.preventDefault();
    event.stopPropagation();
    
    // Grabbing the pre-selected courier and customer info
    const orderId = shipBtn.dataset.orderId;
    const customer = shipBtn.dataset.customer;
    const courier = shipBtn.dataset.courier; 
    
    openMarkShippedModal(orderId, customer, courier);
    
    // Close the dropdown menu
    document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    return;
}
    const markAsShippedBtn = event.target.closest('.mark-as-shipped-btn');
if (markAsShippedBtn) {
    event.preventDefault();
    event.stopPropagation();
    
    // Extract data from the button (Ensure your HTML includes data-courier)
    const orderId = markAsShippedBtn.dataset.orderId;
    const customer = markAsShippedBtn.dataset.customer;
    const courier = markAsShippedBtn.dataset.courier; 
    
    openMarkShippedModal(orderId, customer, courier);
    
    // Close the action menu
    document.querySelectorAll('.action-menu.open').forEach(m => m.classList.remove('open'));
    return;
}
    const addNoteBtn = event.target.closest('.add-note-btn');
    if (addNoteBtn) {
        event.preventDefault();
        event.stopPropagation(); 
        
        const orderId = addNoteBtn.dataset.orderId || addNoteBtn.getAttribute('data-order-id') || "Unknown";
        const customer = addNoteBtn.dataset.customer || addNoteBtn.getAttribute('data-customer') || "Customer";
        
        openAddNoteModal(orderId, customer);
    }
    
    // Also handle the Save button click directly here so we don't need a separate listener
    if (event.target.id === 'addNotesSaveBtn') {
        event.preventDefault();
        saveOrderNote();
    }
}, true);
