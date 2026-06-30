window.currentChallengeId = null;
window.isEditMode = false;

// Open edit challenge modal
function openEditChallengeModal(challengeId, title, description, rules, prizes, goalKm, activityType, startDate, endDate, status, bannerBase64, bannerImageName, bannerImageContentType) {
    window.currentChallengeId = challengeId;
    window.isEditMode = true;
    
    const modal = document.getElementById('launchChallengeModal');
    if (!modal) {
        console.error('Launch challenge modal not found');
        showToast('Cannot edit challenge: Modal not found', true);
        return;
    }
    
    // Safely set modal title
    const modalTitle = document.getElementById('modalTitle');
    if (modalTitle) {
        modalTitle.innerHTML = '<i class="bi bi-pencil-square me-2 text-primary"></i>Edit Challenge';
    }
    
    // Safely set edit challenge ID
    const editChallengeId = document.getElementById('editChallengeId');
    if (editChallengeId) {
        editChallengeId.value = challengeId;
    }
    
    // Safely show/hide status field
    const statusField = document.getElementById('statusField');
    if (statusField) {
        statusField.style.display = 'block';
    }
    
    // Set form values with null checks
    const titleField = document.getElementById('challengeTitle');
    if (titleField) titleField.value = title;
    
    const descField = document.getElementById('challengeDesc');
    if (descField) descField.value = description || '';
    
    const rulesField = document.getElementById('challengeRules');
    if (rulesField) rulesField.value = rules || '';
    
    const prizesField = document.getElementById('challengePrizes');
    if (prizesField) prizesField.value = prizes || '';
    
    const goalField = document.getElementById('goalKm');
    if (goalField) goalField.value = goalKm;
    
    const activityTypeField = document.getElementById('activityType');
    if (activityTypeField) activityTypeField.value = activityType;
    
    const startDateField = document.getElementById('startDate');
    if (startDateField) startDateField.value = startDate ? startDate.split('T')[0] : '';
    
    const endDateField = document.getElementById('endDate');
    if (endDateField) endDateField.value = endDate ? endDate.split('T')[0] : '';
    
    const statusFieldSelect = document.getElementById('challengeStatus');
    if (statusFieldSelect) statusFieldSelect.value = status;
    
    // Handle existing image
    const preview = document.getElementById('imagePreview');
    const placeholder = document.getElementById('uploadPlaceholder');
    
    if (preview && placeholder) {
        if (bannerBase64 && bannerBase64 !== '#' && bannerBase64 !== null && bannerBase64 !== 'null') {
            preview.src = bannerBase64;
            preview.classList.remove('d-none');
            placeholder.classList.add('d-none');
        } else {
            preview.classList.add('d-none');
            preview.src = '#';
            placeholder.classList.remove('d-none');
        }
    }
    
    // Reset file input
    const fileInput = document.getElementById('challengeImgInput');
    if (fileInput) fileInput.value = '';
    
    // Show modal
    const modalInstance = new bootstrap.Modal(modal);
    modalInstance.show();
}

// Open create challenge modal
function openCreateChallengeModal() {
    window.isEditMode = false;
    window.currentChallengeId = null;
    
    const modal = document.getElementById('launchChallengeModal');
    if (!modal) return;
    
    const modalTitle = document.getElementById('modalTitle');
    if (modalTitle) {
        modalTitle.innerHTML = '<i class="bi bi-rocket-takeoff me-2 text-primary"></i>Launch New Challenge';
    }
    
    const editChallengeId = document.getElementById('editChallengeId');
    if (editChallengeId) editChallengeId.value = '';
    
    const statusField = document.getElementById('statusField');
    if (statusField) statusField.style.display = 'none';
    
    const form = document.getElementById('launchForm');
    if (form) form.reset();
    
    const preview = document.getElementById('imagePreview');
    const placeholder = document.getElementById('uploadPlaceholder');
    
    if (preview && placeholder) {
        preview.classList.add('d-none');
        preview.src = '#';
        placeholder.classList.remove('d-none');
    }
    
    const fileInput = document.getElementById('challengeImgInput');
    if (fileInput) fileInput.value = '';
    
    // Reset prize section
    const prizeSection = document.getElementById('prizeManagementSection');
    if (prizeSection) prizeSection.style.display = 'none';
    
    const prizeContainer = document.getElementById('prizeTiersContainer');
    if (prizeContainer) prizeContainer.innerHTML = '';
    
    window.prizeTierCount = 0;
    
    // Reset radio buttons
    const hasPrizesNo = document.getElementById('hasPrizesNo');
    if (hasPrizesNo) hasPrizesNo.checked = true;
    
    const modalInstance = new bootstrap.Modal(modal);
    modalInstance.show();
}

// Preview challenge image
function previewChallengeImage(input) {
    const preview = document.getElementById('imagePreview');
    const placeholder = document.getElementById('uploadPlaceholder');
    
    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function(e) {
            if (preview) {
                preview.src = e.target.result;
                preview.classList.remove('d-none');
            }
            if (placeholder) {
                placeholder.classList.add('d-none');
            }
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// Format date
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-PH', { month: 'short', day: 'numeric', year: 'numeric' });
}

// Format date and time
function formatDateTime(dateString) {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleString('en-PH', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' });
}

// Escape HTML
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Show toast
function showToast(message, isError = false) {
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');
    const toastEl = document.getElementById('challengeToast');
    
    // If toast elements don't exist, use alert as fallback
    if (!toastMsg || !toastEl) {
        if (isError) {
            alert('Error: ' + message);
        } else {
            console.log('Toast: ' + message);
        }
        return;
    }
    
    toastMsg.innerText = message;
    toastIcon.className = isError ? 'bi bi-exclamation-triangle-fill text-danger fs-5' : 'bi bi-check-circle-fill text-success fs-5';
    
    const toast = new bootstrap.Toast(toastEl, { delay: 3000 });
    toast.show();
}

// Format seconds to HH:MM:SS
function formatTime(seconds) {
    if (!seconds || seconds <= 0) return '00:00:00';
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}