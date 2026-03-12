/* --- MY PURCHASES JAVASCRIPT --- */

let currentOrderId = null;

// 1. OPEN MODAL AND POPULATE DATA
function viewOrderDetails(orderId, status, payment, seller, receiver, phone, address, total, color, size, qty, orderDate) {
    currentOrderId = orderId;
    const overlay = document.getElementById('customOrderOverlay');

    // Ensure we are showing the Details view (not tracking or edit) when opening
    resetModalViews();

    // Set Header/Status
    document.getElementById('modalOrderNumber').innerText = orderId;
    document.getElementById('modalStatusText').innerText = status;
    document.getElementById('modalStatusSubtext').innerText = `Seller: ${seller}`;

    // Set Display Info
    document.getElementById('displayReceiver').innerText = receiver;
    document.getElementById('displayPhone').innerText = phone;
    document.getElementById('displayAddress').innerText = address;
    document.getElementById('displayPayment').innerText = `Payment: ${payment}`;
    document.getElementById('displayColor').innerText = color;
    document.getElementById('displaySize').innerText = size;
    document.getElementById('displayQty').innerText = qty;
    document.getElementById('modalTotalAmount').innerText = total;

    // Set Input values (for editing mode)
    document.getElementById('inputReceiver').value = receiver;
    document.getElementById('inputPhone').value = phone;
    document.getElementById('inputAddress').value = address;

    // --- LOCK EDITING LOGIC ---
    const editBtn = document.getElementById('editOrderBtn');
    const lockNote = document.getElementById('editLockNote');

    // Disable editing if the order is "To Receive", "Completed", or "Cancelled"
    if (status === "To Ship" || status === "To Receive" || status === "Completed" || status === "Cancelled") {
        editBtn.style.display = 'none';
        lockNote.classList.remove('d-none');
    } else {
        editBtn.style.display = 'block';
        lockNote.classList.add('d-none');
    }

    // Prepare Timeline dates if tracking is accessed later
    if (orderDate) {
        document.getElementById('date-placed').innerText = orderDate;
    }

    overlay.classList.add('active');
}

// 2. CLOSE MODAL & RESET STATE
function closeOrderDetails() {
    const overlay = document.getElementById('customOrderOverlay');
    overlay.classList.remove('active');

    // Delay reset slightly to prevent flickering during close animation
    setTimeout(resetModalViews, 300);
}

function resetModalViews() {
    // Show standard sections
    document.getElementById('deliverySection').classList.remove('d-none');
    document.getElementById('orderDetailsDisplay').classList.remove('d-none');
    document.getElementById('productDetailsDisplay').classList.remove('d-none');

    // Hide specialized sections
    document.getElementById('orderEditForm').classList.add('d-none');
    document.getElementById('trackingSection').classList.add('d-none');

    // Reset Timeline classes to prevent style carry-over
    const items = document.querySelectorAll('.timeline-item');
    items.forEach(item => {
        item.classList.remove('completed', 'active', 'pending');
        // Reset subtexts to "Pending" by default
        const dateEl = item.querySelector('.timeline-date');
        if (dateEl && !dateEl.id) dateEl.innerText = "Pending";
    });
}

// 3. TOGGLE EDIT MODE
function toggleOrderEdit() {
    const displayDiv = document.getElementById('orderDetailsDisplay');
    const formDiv = document.getElementById('orderEditForm');

    if (formDiv.classList.contains('d-none')) {
        formDiv.classList.remove('d-none');
        displayDiv.classList.add('d-none');
    } else {
        formDiv.classList.add('d-none');
        displayDiv.classList.remove('d-none');
    }
}

// 4. COPY ORDER ID TO CLIPBOARD
function copyOrderId() {
    const orderId = document.getElementById('modalOrderNumber').innerText;
    navigator.clipboard.writeText(orderId).then(() => {
        showToast("Order ID copied to clipboard!", "success");
    }).catch(err => {
        showToast("Failed to copy ID", "error");
    });
}

// 5. SAVE CHANGES — persists to the database
function saveOrderChanges() {
    const newName    = document.getElementById('inputReceiver').value.trim();
    const newPhone   = document.getElementById('inputPhone').value.trim();
    const newAddress = document.getElementById('inputAddress').value.trim();
    const newPayment = document.getElementById('inputPayment').value;

    if (!newName || !newPhone || !newAddress) {
        showToast('Please fill in all required fields.', 'error');
        return;
    }

    fetch('/Home/UpdateOrderShipping', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
            orderNumber:   currentOrderId,
            receiverName:  newName,
            phone:         newPhone,
            address:       newAddress,
            paymentMethod: newPayment
        })
    })
    .then(res => res.json())
    .then(data => {
        if (data.success) {
            document.getElementById('displayReceiver').innerText = newName;
            document.getElementById('displayPhone').innerText    = newPhone;
            document.getElementById('displayAddress').innerText  = newAddress;
            document.getElementById('displayPayment').innerText  = 'Payment: ' + newPayment;
            showToast('Shipping details updated successfully!', 'success');
            toggleOrderEdit();
        } else {
            showToast(data.message || 'Could not save changes.', 'error');
        }
    })
    .catch(() => showToast('Network error. Please try again.', 'error'));
}

// 6. TRACK ORDER LOGIC
function trackOrder(orderId, status, payment, seller, receiver, phone, address, total, color, size, qty, orderDate) {
    // 1. Populate all data into the modal first
    viewOrderDetails(orderId, status, payment, seller, receiver, phone, address, total, color, size, qty, orderDate);

    // 2. Adjust visibility for Tracking Mode
    document.getElementById('deliverySection').classList.add('d-none');      // Hide "Delivery & Payment"
    document.getElementById('productDetailsDisplay').classList.add('d-none'); // Hide "Product Specs"
    document.getElementById('trackingSection').classList.remove('d-none');   // Show "Shipping Journey"
    document.getElementById('editOrderBtn').style.display = 'none';           // Ensure Edit is hidden

    // 3. Update the timeline dots and lines
    updateTimeline(status, orderDate);
}

function updateTimeline(status, orderDate) {
    const items = document.querySelectorAll('.timeline-item');

    // Always mark "Order Placed" as completed
    items[0].classList.add('completed');
    document.getElementById('date-placed').innerText = orderDate;

    if (status === "To Ship") {
        // Step 2 is Active
        items[1].classList.add('active');
        document.getElementById('date-shipped').innerText = "Packed and ready for pickup";

        // Future steps are pending
        items[2].classList.add('pending');
        items[3].classList.add('pending');
    }
    else if (status === "To Receive") {
        // Step 2 is now Finished
        items[1].classList.add('completed');
        document.getElementById('date-shipped').innerText = "Arrived at sorting facility";

        // Step 3 is Active (In Transit)
        items[2].classList.add('active');
        items[2].querySelector('.timeline-date').innerText = "In transit to local hub";

        // Final step is pending
        items[3].classList.add('pending');
    }
    else if (status === "Completed") {
        // All steps are finished
        items[1].classList.add('completed');
        items[2].classList.add('completed');
        items[3].classList.add('completed');
        items[3].querySelector('.timeline-date').innerText = "Delivered successfully";
    }
}

// 7. TOAST SYSTEM
function showToast(message, type) {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `mono-toast ${type}`;

    const icon = type === 'success' ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';

    toast.innerHTML = `
        <i class="bi ${icon}"></i>
        <span>${message}</span>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateY(20px)';
        setTimeout(() => toast.remove(), 500);
    }, 3000);
}