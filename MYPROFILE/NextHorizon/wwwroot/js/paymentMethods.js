﻿/* =============================================================
   PAYMENT METHODS - FINAL SYNCHRONIZED LOGIC
   ============================================================= */

let editingCardId = null;
let currentPaymentType = 'Card';
let addressData = {};
let nextId = 100;

const regionNames = {
    "01": "Region I", "02": "Region II", "03": "Region III", "4A": "Region IV-A",
    "4B": "Region IV-B", "05": "Region V", "06": "Region VI", "07": "Region VII",
    "08": "Region VIII", "09": "Region IX", "10": "Region X", "11": "Region XI",
    "12": "Region XII", "13": "Region XIII", "ARMM": "ARMM", "CAR": "CAR", "NCR": "NCR", "NIR": "NIR"
};

document.addEventListener('DOMContentLoaded', async function () {
    // Remove any existing listeners first
    const form = document.getElementById('paymentForm');
    if (form) {
        // Remove old listener if exists
        form.removeEventListener('submit', handleFormSubmit);
        // Add new listener
        form.addEventListener('submit', function(e) {
            e.preventDefault();
            handleFormSubmit();
        });
    }
    
    await loadAddressData();
});

/* --- CORE FUNCTIONS --- */

async function loadAddressData() {
    try {
        const response = await fetch('/philippine_provinces_cities_municipalities_and_barangays_2016.json');
        addressData = await response.json();
    } catch (e) { console.error("Error loading address data", e); }
}

function toggleView() {
    const viewSection = document.getElementById('payment-view-section');
    const formSection = document.getElementById('payment-form-section');
    const isShowingForm = formSection.style.display === 'none' || formSection.style.display === '';

    viewSection.style.display = isShowingForm ? 'none' : 'block';
    formSection.style.display = isShowingForm ? 'block' : 'none';

    if (isShowingForm) {
        window.scrollTo({ top: 0, behavior: 'smooth' });
        // Reset form when opening
        document.getElementById('paymentForm').reset();
        editingCardId = null;
        setPaymentType('Card');
    }
}

/**
 * UPDATED: Optimized to remove layout gaps by toggling display correctly
 */
function setPaymentType(type) {
    currentPaymentType = type;
    document.getElementById('selectedPaymentType').value = type;

    const cardCol = document.getElementById('card-left-column');
    const walletFields = document.getElementById('ewallet-fields');
    const billingSection = document.getElementById('billing-address-section');
    const billingLabels = billingSection ? billingSection.querySelectorAll('label') : [];

    if (type === 'Card') {
        // Show card fields
        if (cardCol) {
            cardCol.style.display = 'block';
            cardCol.querySelectorAll('input, select').forEach(el => {
                el.disabled = false;
                el.required = true;
            });
        }
        
        // Show billing address (required for cards)
        if (billingSection) {
            billingSection.style.display = 'block';
            billingSection.querySelectorAll('input, select').forEach(el => {
                el.disabled = false;
                el.required = true;
            });
        }
        
        // Hide wallet fields
        if (walletFields) {
            walletFields.style.display = 'none';
            walletFields.querySelectorAll('input, select').forEach(el => {
                el.disabled = true;
                el.required = false;
            });
        }
    } else {
        // Hide card fields
        if (cardCol) {
            cardCol.style.display = 'none';
            cardCol.querySelectorAll('input, select').forEach(el => {
                el.disabled = true;
                el.required = false;
            });
        }
        
        // OPTION 1: Hide billing address completely for e-wallets
        if (billingSection) {
            billingSection.style.display = 'none';
            billingSection.querySelectorAll('input, select').forEach(el => {
                el.disabled = true;
                el.required = false;
            });
        }

        // Show wallet fields
        if (walletFields) {
            walletFields.style.display = 'block';
            walletFields.querySelectorAll('input, select').forEach(el => {
                el.disabled = false;
                el.required = true;
            });
        }
    }

    // Update active state of toggle buttons
    document.getElementById('type-card').classList.toggle('active', type === 'Card');
    document.getElementById('type-ewallet').classList.toggle('active', type === 'EWallet');
}

/* --- CRUD ACTIONS --- */

// In your prepareAddForm function
function prepareAddForm() {
    // Prevent multiple clicks
    if (window.isPreparingForm) return;
    window.isPreparingForm = true;
    
    editingCardId = null;
    const form = document.getElementById('paymentForm');
    form.reset();
    
    const methodIdField = document.getElementById('methodId');
    if (methodIdField) methodIdField.value = "";
    
    // Reset all border colors
    form.querySelectorAll('input, select').forEach(el => {
        el.style.borderColor = "#ccc";
        el.disabled = false;
    });

    setPaymentType('Card');
    
    const defaultCheckbox = document.getElementById('defaultToggle');
    if (defaultCheckbox) defaultCheckbox.checked = false;
    
    document.querySelector('.btn-submit').innerText = "SAVE PAYMENT METHOD";
    toggleView();
    
    // Reset the flag after a short delay
    setTimeout(() => {
        window.isPreparingForm = false;
    }, 500);
}

function prepareEditForm(data) {
     // Get the ID - handle both camelCase and PascalCase property names
    editingCardId = data.payoutAccountId || data.PayoutAccountId;
    
    console.log('Editing ID:', editingCardId); // Debug log
    
    const type = data.accountType || data.AccountType || 'Card';
    
    toggleView();
    setPaymentType(type);

    const form = document.getElementById('paymentForm');
    
    // Clear form first
    form.reset();
    
    // Set the ID field
    const methodIdField = document.getElementById('methodId');
    if (methodIdField) {
        methodIdField.value = editingCardId;
        console.log('Set methodId to:', editingCardId); // Debug log
    }
    // Set common fields
    setFieldValue('AccountName', data.accountName || data.AccountName || '');
    setFieldValue('Region', data.region || data.Region || '');
    setFieldValue('Province', data.province || data.Province || '');
    setFieldValue('City', data.city || data.City || '');
    setFieldValue('Barangay', data.barangay || data.Barangay || '');
    setFieldValue('PostalCode', data.postalCode || data.PostalCode || '');
    setFieldValue('StreetName', data.streetName || data.StreetName || '');
    setFieldValue('Building', data.building || data.Building || '');
    setFieldValue('HouseNo', data.houseNo || data.HouseNo || '');
    
    // Set default checkbox
    const defaultCheckbox = document.getElementById('defaultToggle');
    if (defaultCheckbox) {
        defaultCheckbox.checked = data.isDefault || data.IsDefault || false;
    }

    if (type === 'Card') {
        setFieldValue('CardNumber', data.cardNumber || data.CardNumber || '');
        setFieldValue('ExpirationMonth', data.expirationMonth || data.ExpirationMonth || '');
        setFieldValue('ExpirationYear', data.expirationYear || data.ExpirationYear || '');
        setFieldValue('CvvCode', ''); // Don't show CVV for security
    } else {
        setFieldValue('BankName', data.bankName || data.BankName || 'GCash');
        setFieldValue('AccountNumber', data.accountNumber || data.AccountNumber || '');
    }

    document.querySelector('.btn-submit').innerText = "UPDATE PAYMENT METHOD";
}

// Helper function to set field values safely
function setFieldValue(fieldName, value) {
    const field = document.querySelector(`[name="${fieldName}"]`);
    if (field) {
        field.value = value || '';
    }
}

// In paymentMethods.js, update the handleFormSubmit function
// Add a flag to prevent double submission
let isSubmitting = false;

async function handleFormSubmit() {
    // Prevent double submission
    if (isSubmitting) {
        console.log('Already submitting, please wait...');
        return;
    }
    
    isSubmitting = true;
    
    // Disable the submit button to prevent double-click
    const submitBtn = document.querySelector('.btn-submit');
    const originalText = submitBtn.innerText;
    submitBtn.disabled = true;
    submitBtn.innerText = 'SAVING...';

    const form = document.getElementById('paymentForm');
    const formData = new FormData(form);
    const type = document.getElementById('selectedPaymentType').value;
    let isValid = true;

    // Reset all border colors first
    form.querySelectorAll('input, select').forEach(el => {
        el.style.borderColor = "#ccc";
    });

    // Validation Helper
    const check = (name, required = true) => {
        const el = form.querySelector(`[name="${name}"]`);
        if (!el) return true;
        
        // Skip validation if element is hidden/disabled
        if (el.closest('#card-left-column') && type !== 'Card') {
            return true;
        }
        if (el.closest('#ewallet-fields') && type !== 'EWallet') {
            return true;
        }
        
        const val = el.value.trim();
        const isInvalid = required && (!val || val === "MM" || val === "YYYY" || val === "");
        if (isInvalid) {
            el.style.borderColor = "black";
            isValid = false;
        }
        return !isInvalid;
    };

    // Validate based on payment type
    if (type === 'Card') {
        // Card-specific fields
        check("AccountName");
        check("CardNumber");
        check("ExpirationMonth");
        check("ExpirationYear");
        check("CvvCode");
        
        // Billing address is required for cards
        ["Region", "Province", "City", "Barangay", "PostalCode", "StreetName", "HouseNo"].forEach(f => check(f));
    } else {
        // E-Wallet specific fields
        check("AccountName");
        check("BankName");
        check("AccountNumber");
        
        
    }

    if (!isValid) {
        Swal.fire({ 
            title: 'REQUIRED FIELDS', 
            text: type === 'Card' ? 'Please fill in all card and billing fields.' : 'Please fill in all e-wallet fields.',
            icon: 'warning', 
            iconColor: '#000', 
            confirmButtonColor: '#000' 
        });
        
        // Reset submission flag
        isSubmitting = false;
        submitBtn.disabled = false;
        submitBtn.innerText = originalText;
        return;
    }

    // Get the anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    // Convert FormData to a plain object
    const formObject = {};
    formData.forEach((value, key) => {
        // Handle checkboxes
        if (key === 'IsDefault') {
            formObject[key] = value === 'on';
        }
        // Handle UserId - convert to number
        else if (key === 'UserId') {
            formObject[key] = parseInt(value, 10);
        }
        // Handle PostalCode - convert to number if not empty
        else if (key === 'PostalCode' && value !== '') {
            formObject[key] = parseInt(value, 10);
        }
        // Handle PayoutAccountId - convert to number if present
        else if (key === 'PayoutAccountId' && value !== '' && value !== null) {
            formObject[key] = parseInt(value, 10);
        }
        // Handle empty strings
        else if (value === '') {
            formObject[key] = null;
        }
        else {
            formObject[key] = value;
        }
    });

    // Get the methodId field value
    const methodIdField = document.getElementById('methodId');
    if (methodIdField && methodIdField.value) {
        formObject['PayoutAccountId'] = parseInt(methodIdField.value, 10);
    }

    // Ensure AccountType is set correctly
    formObject['AccountType'] = type;

    // Remove card fields if this is e-wallet
    if (type === 'EWallet') {
        delete formObject['CardNumber'];
        delete formObject['ExpirationMonth'];
        delete formObject['ExpirationYear'];
        delete formObject['CvvCode'];
    } else {
        delete formObject['BankName'];
        delete formObject['AccountNumber'];
    }

    console.log('Sending data:', JSON.stringify(formObject, null, 2));

    try {
        const response = await fetch('/AccountProfile/SavePaymentMethod', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(formObject)
        });

        if (!response.ok) {
            const text = await response.text();
            console.error('Server response:', text);
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        console.log('Server result:', result);

        if (result.success) {
            await Swal.fire({
                title: editingCardId ? 'UPDATED' : 'SAVED',
                text: result.message,
                icon: 'success',
                iconColor: '#000',
                confirmButtonColor: '#000',
                timer: 1500,
                showConfirmButton: false
            });
            
            window.location.reload();
        } else {
            Swal.fire({
                title: 'ERROR',
                text: result.message,
                icon: 'error',
                iconColor: '#000',
                confirmButtonColor: '#000'
            });
            
            isSubmitting = false;
            submitBtn.disabled = false;
            submitBtn.innerText = originalText;
        }
    } catch (error) {
        console.error('Fetch error:', error);
        Swal.fire({
            title: 'ERROR',
            text: 'An error occurred while saving. Check console for details.',
            icon: 'error',
            iconColor: '#000',
            confirmButtonColor: '#000'
        });
        
        isSubmitting = false;
        submitBtn.disabled = false;
        submitBtn.innerText = originalText;
    }
}
function renderPaymentMethodCard(method) {
    const icon = method.type === 'Card' ? 'bi-credit-card-2-front' : 'bi-wallet2';
    const detailTitle = method.type === 'Card' ? `${method.brand} Ending in ${method.last4}` : `${method.brand} Account`;
    const detailSub = method.type === 'Card' ? `Expires ${method.expiry}` : method.account;

    return `
        <div class="saved-card-item ${method.isDefault ? 'active-selection' : ''}" id="method-${method.id}">
            <div class="card-info-left">
                <i class="bi ${icon}"></i>
                <div class="card-details">
                    <span class="card-brand">${detailTitle}</span>
                    <span class="card-expiry">${detailSub}</span>
                </div>
            </div>
            <div class="card-actions-right">
                <button class="btn-edit-card" onclick='prepareEditForm(${JSON.stringify(method)})'>
                    <i class="bi bi-pencil"></i>
                </button>
                ${method.isDefault ? '<span class="badge-default">DEFAULT</span>' : `<button class="btn-set-default" onclick="setDefault(${method.id})">SET DEFAULT</button>`}
                <button class="btn-remove" onclick="removeCard(${method.id})">Remove</button>
            </div>
        </div>`;
}

/* --- UI HELPERS --- */

function applyDefaultUI(id) {
    document.querySelectorAll('.saved-card-item').forEach(item => {
        item.classList.remove('active-selection');
        const badge = item.querySelector('.badge-default');
        if (badge) {
            const actionDiv = item.querySelector('.card-actions-right');
            badge.remove();
            const btn = document.createElement('button');
            btn.className = 'btn-set-default';
            btn.innerText = 'SET DEFAULT';
            const itemId = item.id.replace('method-', '');
            btn.onclick = function () { setDefault(itemId); };
            actionDiv.insertBefore(btn, actionDiv.querySelector('.btn-remove'));
        }
    });

    const target = document.getElementById(`method-${id}`);
    if (!target) return;
    target.classList.add('active-selection');
    const actionDiv = target.querySelector('.card-actions-right');
    const setBtn = actionDiv.querySelector('.btn-set-default');
    if (setBtn) setBtn.remove();

    const badge = document.createElement('span');
    badge.className = 'badge-default';
    badge.innerText = 'DEFAULT';
    actionDiv.insertBefore(badge, actionDiv.querySelector('.btn-remove'));
}

async function setDefault(id) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    Swal.fire({
        title: 'SET AS DEFAULT?',
        text: 'Do you want to set this as your default payment method?',
        icon: 'question',
        iconColor: '#000',
        showCancelButton: true,
        confirmButtonColor: '#000',
        cancelButtonColor: '#666'
    }).then(async (result) => {
        if (result.isConfirmed) {
            try {
                const response = await fetch(`/AccountProfile/SetDefaultPaymentMethod/${id}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                });

                const data = await response.json();

                if (data.success) {
                    Swal.fire({
                        title: 'DEFAULT UPDATED',
                        text: data.message,
                        icon: 'success',
                        iconColor: '#000',
                        confirmButtonColor: '#000',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.reload();
                    });
                }
            } catch (error) {
                console.error('Error:', error);
            }
        }
    });
}


async function removeCard(id) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    Swal.fire({
        title: 'REMOVE?',
        text: 'This will permanently delete this payment method.',
        icon: 'warning',
        iconColor: '#000',
        showCancelButton: true,
        confirmButtonColor: '#000',
        cancelButtonColor: '#666',
        confirmButtonText: 'YES, DELETE'
    }).then(async (result) => {
        if (result.isConfirmed) {
            try {
                const response = await fetch(`/AccountProfile/DeletePaymentMethod/${id}`, {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token,
                        'Content-Type': 'application/json'
                    }
                });

                const data = await response.json();

                if (data.success) {
                    Swal.fire({
                        title: 'DELETED',
                        text: data.message,
                        icon: 'success',
                        iconColor: '#000',
                        confirmButtonColor: '#000',
                        timer: 1500,
                        showConfirmButton: false
                    }).then(() => {
                        window.location.reload();
                    });
                }
            } catch (error) {
                console.error('Error:', error);
            }
        }
    });
}