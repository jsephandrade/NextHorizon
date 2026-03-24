let activeStatusFilter = 'all';
let selectedNoteOrderId = '';
let selectedNoteCustomer = '';
let selectedReviewOrderId = '';
let selectedShipmentOrderId = '';
let selectedReturnOrderId = '';

// ================= UTILS =================
function applyOrderFilters() {
    const searchValue = (document.getElementById('orderSearch')?.value || '').toLowerCase().trim();
    const categoryValue = document.getElementById('categoryFilter')?.value || 'all';

    document.querySelectorAll('.order-row').forEach(function (row) {
        const status = row.dataset.status || '';
        const rowText = row.innerText.toLowerCase();

        const statusMatch = activeStatusFilter === 'all' || status === activeStatusFilter;
        const categoryMatch = categoryValue === 'all';
        const searchMatch = !searchValue || rowText.includes(searchValue);

        row.style.display = (statusMatch && categoryMatch && searchMatch) ? '' : 'none';
    });
}
// 1. Opens the premium modal instead of the browser prompt
function declineOrder() {
    // Grab the ID from the main summary modal
    const orderIdText = document.getElementById("summaryOrderId").innerText;
    
    // Put that ID into the new decline modal header
    document.getElementById("declineModalOrderId").innerText = orderIdText;
    
    // Hide the main modal, show the new decline modal
    document.getElementById("orderSummaryModal").style.display = "none";
    document.getElementById("declineOrderModal").style.display = "flex";
}

// 2. Closes the decline modal and goes back to the summary
function closeDeclineModal() {
    document.getElementById("declineReasonSelect").value = ""; // Reset dropdown
    document.getElementById("declineOrderModal").style.display = "none";
    document.getElementById("orderSummaryModal").style.display = "flex";
}

// 3. Actually sends the data to the server
async function submitDeclineOrder() {
    const orderIdText = document.getElementById("declineModalOrderId").innerText; 
    const orderId = parseInt(orderIdText.replace("ORD-", "").trim()); 
    const reason = document.getElementById("declineReasonSelect").value;

    if (!reason) {
        alert("Please select a valid reason from the dropdown.");
        return;
    }

    try {
        const response = await fetch('/Dashboard/DeclineOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ OrderId: orderId, Reason: reason })
        });

        const result = await response.json();

        if (result.success) {
            // Success! Refresh the page to move order to Cancelled tab
            location.reload(); 
        } else {
            alert("Failed: " + result.message);
        }
    } catch (error) {
        console.error("Server error:", error);
        alert("A network error occurred. Please check the terminal.");
    }
}
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

function openMarkShippedModal(orderRow) {
    if (!orderRow) return;

    selectedShipmentOrderId = orderRow.dataset.orderId || '';
    document.getElementById('shipOrderId').textContent = selectedShipmentOrderId;
    document.getElementById('shipCustomer').textContent = orderRow.children[1]?.innerText || '---';
    document.getElementById('shipItems').textContent = orderRow.children[4]?.innerText || '0';
    document.getElementById('shipTotal').textContent = orderRow.children[5]?.innerText || '0.00';
    document.getElementById('courierName').value = 'J&T Express';
    document.getElementById('trackingNumber').value = '';
    document.getElementById('shipmentDate').value = new Date().toISOString().split('T')[0];
    document.getElementById('shipmentNotes').value = '';
    document.getElementById('markShippedModal').style.display = 'flex';
}

function closeMarkShippedModal() {
    document.getElementById('markShippedModal').style.display = 'none';
}

function openConfirmShipmentModal() {
    document.getElementById('confirmShipOrderId').textContent = selectedShipmentOrderId || '';
    document.getElementById('confirmShipmentModal').style.display = 'flex';
}

function closeConfirmShipmentModal() {
    document.getElementById('confirmShipmentModal').style.display = 'none';
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

// ================= MODALS =================
function openAddNotesModal(orderId, customer) {
    selectedNoteOrderId = orderId || '';
    selectedNoteCustomer = customer || '';

    // Header: "Customer: name - order #"
    document.getElementById('customerHeaderText').textContent = `Customer: ${customer} - Order #${orderId}`;

    // Customer card (beside avatar)
    document.getElementById('addNotesCustomerText').textContent = `${customer}`;

    // Clear textarea
    document.getElementById('addNotesTextarea').value = '';

    // Show modal
    document.getElementById('addNotesModal')?.classList.add('active');
}

function closeAddNotesModal() {
    const modal = document.getElementById('addNotesModal');
    modal?.classList.remove('active');
    selectedNoteOrderId = '';
    selectedNoteCustomer = '';
}

function openNoteSavedModal(orderId, noteText) {
    const modal = document.getElementById('noteSavedModal');
    const orderLabel = document.getElementById('noteSavedOrderId');
    const message = document.getElementById('noteSavedMessage');
    if (orderLabel) {
        orderLabel.textContent = orderId || '';
    }
    if (message) {
        message.textContent = noteText.trim();
    }
    modal?.classList.add('active');
}

function closeNoteSavedModal() {
    const modal = document.getElementById('noteSavedModal');
    modal?.classList.remove('active');
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

    // Add note
    const addNoteBtn = event.target.closest('.add-note-btn');
    if (addNoteBtn) {
        event.stopPropagation(); // 🔥 prevents interference
        openAddNotesModal(addNoteBtn.dataset.orderId, addNoteBtn.dataset.customer);
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
    if (event.target.id === 'noteSavedModal') closeNoteSavedModal();
    if (event.target.id === 'markShippedModal') closeMarkShippedModal();
    if (event.target.id === 'confirmShipmentModal') closeConfirmShipmentModal();
    if (event.target.id === 'trackingRequiredModal') closeTrackingRequiredModal();
    if (event.target.id === 'returnedInfoModal') closeReturnedInfoModal();
    if (event.target.id === 'confirmReturnModal') closeConfirmReturnModal();
});

document.getElementById('addNotesSaveBtn')?.addEventListener('click', function () {
    const textarea = document.getElementById('addNotesTextarea');
    const value = (textarea?.value || '').trim();

    if (value) {
        // TODO: Save to backend
        closeAddNotesModal();
        openNoteSavedModal(selectedNoteOrderId, value);
    }
});

document.getElementById('addNotesCancelBtn')?.addEventListener('click', closeAddNotesModal);
document.getElementById('reviewRequestCancelBtn')?.addEventListener('click', closeReviewRequestModal);
document.getElementById('noteSavedOkBtn')?.addEventListener('click', closeNoteSavedModal);
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

// ===== DECLINE =====
function openDeclineModal() {
    alert("Decline reason modal (you can build next)");
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

function completeShipment() {
    updateOrderRowStatus(selectedShipmentOrderId, 'shipped', 'Shipped');
    closeConfirmShipmentModal();
    closeMarkShippedModal();
    applyOrderFilters();
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
            // (Optional: You can also replace these alerts with nice UI toast notifications later!)
            alert("Success: " + result.message);
            location.reload(); // Refresh to move the order to the 'To Ship' tab
        } else {
            alert("Failed: " + result.message);
        }
    } catch (error) {
        console.error("Server error:", error);
        alert("A network error occurred. Please try again. See console for details.");
    }
}

function hideCourierWarning() {
    const warningEl = document.getElementById("courierWarning");
    const selectEl = document.getElementById("courierSelect");
    
    if (warningEl) warningEl.style.display = "none";
    if (selectEl) selectEl.classList.remove("input-error");
}
async function declineOrder() {
    // 1. Grab the Order ID from the open Order Summary Modal
    const orderIdElement = document.getElementById("modalOrderId") || document.getElementById("summaryOrderId");
    if (!orderIdElement) {
        alert("Unable to find order ID to decline. Refresh the page and try again.");
        return;
    }

    const orderIdText = orderIdElement.innerText;
    const orderId = Number.parseInt(orderIdText.replace("ORD-", "").trim(), 10);
    if (Number.isNaN(orderId)) {
        alert("Invalid order ID. Cannot decline order.");
        return;
    }

    // 2. Ask the seller for the reason
    const reason = prompt("Please enter the reason for declining this order (e.g., Out of stock, Invalid address):");

    // 3. Validation: Stop if they hit cancel or left it empty
    if (reason === null) {
        return; 
    }
    if (reason.trim() === "") {
        alert("You must provide a reason to decline an order.");
        return;
    }

    // 4. Send the ID and Reason to your C# Controller
    try {
        const response = await fetch('/Dashboard/DeclineOrder', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                OrderId: orderId,
                Reason: reason
            })
        });

        if (!response.ok) {
            const text = await response.text();
            throw new Error(`HTTP ${response.status}: ${text}`);
        }

        const result = await response.json();

        if (result.success) {
            alert("Order Cancelled: " + result.message);
            location.reload(); // Refresh to move the order to the 'Cancelled' tab
        } else {
            alert("Failed: " + result.message);
        }
    } catch (error) {
        console.error("Server error:", error);
        alert("A network error occurred. Please try again. See console for details.");
    }
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
            const subtotal = order.subtotal || order.Subtotal || 0;
            const shipping = order.shippingFee || order.ShippingFee || 0;
            const grandTotal = order.totalAmount || order.TotalAmount || 0;

            const totItemsEl = document.getElementById("modalTotalItems");
            if (totItemsEl) totItemsEl.innerText = qty;

            const subtotalEl = document.getElementById("modalSubtotal");
            if (subtotalEl) subtotalEl.innerText = subtotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });

            const shipEl = document.getElementById("modalShippingFee");
            if (shipEl) shipEl.innerText = shipping.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });

            const grandEl = document.getElementById("modalGrandTotal");
            if (grandEl) grandEl.innerText = grandTotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' });

            // 4. Update the Items Table with Variant Data
            const colors = order.colors || order.Colors;
            const variantText = colors ? `Color: ${colors}` : "Standard Variant";
            const productName = order.productName || order.ProductName || "Product Name Unavailable";

            const itemsBodyEl = document.getElementById("modalItemsBody");
            if (itemsBodyEl) {
                itemsBodyEl.innerHTML = `
                    <tr>
                        <td>
                            <strong style="display: block; color: #111827;">${productName}</strong>
                            <span style="font-size: 0.8rem; color: #6b7280;">${variantText}</span>
                        </td>
                        <td style="text-align: center; font-weight: 500;">x${qty}</td>
                        <td style="text-align: right; font-weight: 500;">${subtotal.toLocaleString('en-PH', { style: 'currency', currency: 'PHP' })}</td>
                    </tr>
                `;
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

