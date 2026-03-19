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
    // ===== VIEW ORDER (PUT THIS FIRST) =====
    const viewOrderBtn = event.target.closest('.view-order-btn');
    if (viewOrderBtn) {
        event.stopPropagation();

        const orderId = viewOrderBtn.dataset.orderId;

        console.log("Opening modal:", orderId); // DEBUG

        openOrderSummaryModal(orderId);

        return;
    }
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
    document.getElementById('orderSummaryModal').classList.remove('active');
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
