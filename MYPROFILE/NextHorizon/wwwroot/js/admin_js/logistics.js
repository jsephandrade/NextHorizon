﻿﻿/**
 * Logistics Partner Module - Admin Logic
 * Handles Search, Filtering, Modals, and SweetAlert2 Actions with Reason Requirements
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Set default view to 'Active'
    filterPartners('active');

    // 2. Unified Search Functionality
    const searchInput = document.getElementById('logisticsSearch');
    if (searchInput) {
        searchInput.addEventListener('keyup', function () {
            const filter = this.value.toLowerCase();
            const cards = document.querySelectorAll('.partner-card');

            cards.forEach(card => {
                const nameAttr = card.getAttribute('data-name')?.toLowerCase();
                const nameText = card.querySelector('.courier-name')?.innerText.toLowerCase();
                const name = nameAttr || nameText;

                if (name && name.includes(filter)) {
                    card.style.display = "";
                    card.classList.add('animate__animated', 'animate__fadeIn');
                } else {
                    card.style.display = "none";
                }
            });
        });
    }
});

/**
 * Filter View Logic (Total / Active / Inactive / Archived)
 */
function filterPartners(filter) {
    const activeSection = document.getElementById('activeSection');
    const inactiveSection = document.getElementById('inactiveSection');
    const archiveSection = document.getElementById('archiveSection');
    const filterCards = document.querySelectorAll('.filter-card');

    // 1. Update UI Visuals
    filterCards.forEach(card => card.classList.remove('active-filter', 'shadow'));
    const activeCard = document.getElementById(`stat-${filter}`);
    if (activeCard) activeCard.classList.add('active-filter', 'shadow');

    // 2. Reset visibility
    if (activeSection) activeSection.style.display = 'none';
    if (inactiveSection) inactiveSection.style.display = 'none';
    if (archiveSection) archiveSection.style.display = 'none';

    // 3. Toggle Section Visibility
    switch (filter) {
        case 'total':
            activeSection.style.display = 'block';
            inactiveSection.style.display = 'block';
            break;
        case 'active':
            activeSection.style.display = 'block';
            break;
        case 'inactive':
            inactiveSection.style.display = 'block';
            break;
        case 'archived':
            archiveSection.style.display = 'block';
            break;
    }
}

/**
 * Handle Status Toggle (Enable/Disable) with Reason
 */
function handleStatusToggle(name, element, forceEnable = false) {
    const isActivating = forceEnable || (element && element.checked);
    const actionText = isActivating ? 'Enable' : 'Disable';
    const actionColor = isActivating ? '#198754' : '#000';

    Swal.fire({
        title: `${actionText} ${name}?`,
        text: `Please provide a reason for ${isActivating ? 'restoring' : 'suspending'} this service:`,
        input: 'text',
        inputPlaceholder: 'e.g., Maintenance complete / Reported delivery issues',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: actionColor,
        cancelButtonColor: '#d33',
        confirmButtonText: `Confirm ${actionText}`,
        inputValidator: (value) => {
            if (!value) {
                return 'A reason is required to proceed!';
            }
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const reason = result.value;
            console.log(`Action: ${actionText}, Partner: ${name}, Reason: ${reason}`);

            Swal.fire({
                title: 'Success',
                text: `${name} status updated. Reason logged: ${reason}`,
                icon: 'success',
                confirmButtonColor: '#000'
            });
            // AJAX call would happen here
        } else if (element && element.type === "checkbox") {
            element.checked = !isActivating; // Revert switch if cancelled
        }
    });
}

/**
 * Handle Deletion/Archiving with Reason
 */
function confirmDeletePartner(name) {
    Swal.fire({
        title: `Archive ${name}?`,
        html: `<p class="text-danger small">This courier will be removed from seller options.</p>`,
        input: 'textarea',
        inputLabel: 'Reason for Deletion',
        inputPlaceholder: 'Enter the reason for archiving this partner...',
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#000',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Archive Partner',
        inputValidator: (value) => {
            if (!value) {
                return 'You must provide a reason for archiving!';
            }
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const reason = result.value;
            console.log(`Archiving ${name}. Reason: ${reason}`);

            Swal.fire({
                title: 'Archived!',
                text: 'The partner has been moved to archives.',
                icon: 'success',
                confirmButtonColor: '#000'
            });
            // AJAX call to set IsDeleted = true
        }
    });
}

/**
 * Restore Logic
 */
function restorePartner(name) {
    Swal.fire({
        title: 'Restore Partner?',
        text: `Move ${name} back to the main list?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#000',
        confirmButtonText: 'Yes, Restore'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire('Restored!', `${name} is now back in the list.`, 'success');
            // AJAX call: IsDeleted = false
        }
    });
}

/**
 * Modal & Visual Helpers
 */
function openAddPartnerModal() {
    const modalElement = document.getElementById('addPartnerModal');
    if (modalElement) {
        const addModal = new bootstrap.Modal(modalElement);
        document.getElementById('newPartnerName').value = '';
        document.getElementById('logoPreview').src = '/images/default-logo.png';
        addModal.show();
    }
}

function previewImage(input) {
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = e => document.getElementById('logoPreview').src = e.target.result;
        reader.readAsDataURL(input.files[0]);
    }
}

function viewPerformance(name, rate) {
    document.getElementById('perfName').innerText = name;
    document.getElementById('perfRateText').innerText = rate + '%';
    const progressBar = document.getElementById('perfProgressBar');
    progressBar.style.width = '0%';
    setTimeout(() => { progressBar.style.width = rate + '%'; }, 200);

    new bootstrap.Modal(document.getElementById('performanceModal')).show();
}

/**
* Handles Restoring from Archive with a required reason
*/
function handleRestoreWithReason(name) {
    Swal.fire({
        title: 'Restore Partner?',
        text: `Enter the reason for bringing ${name} back to service:`,
        input: 'text',
        inputPlaceholder: 'e.g., Contract renewed / Service restored',
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#198754', // Success Green
        confirmButtonText: 'Yes, Restore Partner',
        inputValidator: (value) => {
            if (!value) {
                return 'You must provide a reason to restore this partner!';
            }
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const reason = result.value;
            console.log(`Restoring ${name}. Reason: ${reason}`);

            Swal.fire({
                title: 'Restored!',
                text: `${name} is now back in the active list.`,
                icon: 'success',
                confirmButtonColor: '#000'
            });

            // Backend AJAX call: IsDeleted = false, LogReason = reason
        }
    });
}