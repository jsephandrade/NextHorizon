document.addEventListener('DOMContentLoaded', function () {
    const editUsernameBtn = document.getElementById('editUsernameBtn');
    const changeAvatarBtn = document.getElementById('changeAvatarBtn');
    const usernameModal = document.getElementById('usernameModal');
    const avatarModal = document.getElementById('avatarModal');

    const avatarInput = document.getElementById('avatarInput');
    const previewImg = document.getElementById('previewImg');
    const avatarPreview = document.getElementById('avatarPreview');
    const uploadText = document.getElementById('uploadText');
    const confirmUploadBtn = document.getElementById('confirmUploadBtn');

    // --- MODAL CONTROLS ---
    if (editUsernameBtn) editUsernameBtn.onclick = () => usernameModal.style.display = 'flex';
    if (changeAvatarBtn) changeAvatarBtn.onclick = () => avatarModal.style.display = 'flex';

    // --- FILE SELECTION & PREVIEW ---
    if (avatarInput) {
        avatarInput.onchange = function () {
            const file = this.files[0];
            if (file) {
                // Truncate long filenames for display
                const fileName = file.name.length > 25 ? file.name.substring(0, 22) + "..." : file.name;
                uploadText.textContent = `Selected: ${fileName}`;

                const reader = new FileReader();
                reader.onload = (e) => {
                    if (previewImg) {
                        previewImg.src = e.target.result;
                        avatarPreview.style.display = 'block';
                    }
                }
                reader.readAsDataURL(file);

                // Hide error message if user finally selects a file
                const errorMessage = document.getElementById('uploadErrorMessage');
                if (errorMessage) errorMessage.style.display = 'none';
            }
        };
    }

    // --- UPLOAD LOGIC (REPLACED ALERT WITH TOAST) ---
    if (confirmUploadBtn) {
        confirmUploadBtn.onclick = function () {
            const fileInput = document.getElementById('avatarInput');

            if (fileInput.files.length > 0) {
                // 1. Loading State
                confirmUploadBtn.textContent = "UPLOADING...";
                confirmUploadBtn.disabled = true;

                // 2. Simulate Upload
                setTimeout(() => {
                    showToast("Profile picture updated successfully!");
                    closeModal('avatarModal');

                    // Reset Button
                    confirmUploadBtn.textContent = "UPLOAD";
                    confirmUploadBtn.disabled = false;
                }, 1500);

            } else {
                // 3. Error State (Red Toast)
                showToast("Please select an image first.", true);

                // Optional: Shake effect on the upload box
                const uploadArea = document.querySelector('.upload-area');
                if (uploadArea) {
                    uploadArea.style.borderColor = '2px solid #000';
                    setTimeout(() => { uploadArea.style.borderColor = '2px solid #000'; }, 2000);
                }
            }
        };
    }

    // Close on outside click
    window.onclick = (event) => {
        if (event.target === usernameModal) closeModal('usernameModal');
        if (event.target === avatarModal) closeModal('avatarModal');
    };
});

// --- GLOBAL FUNCTIONS ---

function closeModal(id) {
    const modal = document.getElementById(id);
    if (modal) {
        modal.style.display = 'none';

        // Reset Avatar Modal specific fields
        if (id === 'avatarModal') {
            const avatarPreview = document.getElementById('avatarPreview');
            const uploadText = document.getElementById('uploadText');
            const avatarInput = document.getElementById('avatarInput');
            const errorMessage = document.getElementById('uploadErrorMessage');

            if (avatarPreview) avatarPreview.style.display = 'none';
            if (uploadText) uploadText.textContent = "Click to upload or drag and drop";
            if (avatarInput) avatarInput.value = "";
            if (errorMessage) errorMessage.style.display = 'none';
        }
    }
}

async function saveUsername() {

    // Get elements and log their state at different times
    const elements = {
        input: document.getElementById('newUsernameInput'),
        span: document.getElementById('usernameDisplay'),
        h1: document.querySelector('.user-details h1')
    };
    
    if (!elements.input || elements.input.value.trim() === "") {
        showToast("ERROR: USERNAME CANNOT BE EMPTY", true);
        if (elements.input) elements.input.style.borderColor = "#000";
        return;
    }

    const newUsername = elements.input.value.trim().toUpperCase();

    try {
        const saveBtn = document.querySelector('.save-btn');
        if (saveBtn) {
            saveBtn.textContent = "SAVING...";
            saveBtn.disabled = true;
        }

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        
        const headers = {
            'Content-Type': 'application/json',
        };
        
        if (token) {
            headers['RequestVerificationToken'] = token;
        }
        
        const response = await fetch('/AccountProfile/UpdateUsername', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({ username: newUsername })
        });

        const result = await response.json();

        if (result.success) {
            // Try multiple times with delay to ensure DOM is ready
            setTimeout(() => {
                const span = document.getElementById('usernameDisplay');
                const h1 = document.querySelector('.user-details h1');
                
                if (span) {
                    span.textContent = newUsername + " ";
                } else if (h1) {
                    const pencilIcon = h1.querySelector('.bi-pencil');
                    h1.innerHTML = '';
                    h1.appendChild(document.createTextNode(newUsername + ' '));
                    if (pencilIcon) {
                        h1.appendChild(pencilIcon);
                    }
                }
            }, 100);
            
            showToast("USERNAME UPDATED");
            closeModal('usernameModal');
            elements.input.value = "";
            elements.input.style.borderColor = "#eee";
        } else {
            showToast("ERROR: " + result.message, true);
        }
    } catch (error) {
        console.error('Error:', error);
        showToast("ERROR: " + error.message, true);
    } finally {
        const saveBtn = document.querySelector('.save-btn');
        if (saveBtn) {
            saveBtn.textContent = "SAVE";
            saveBtn.disabled = false;
        }
    }
}


/**
 * Enhanced Toast Notification
 * @param {string} message - The text to show
 * @param {boolean} isError - If true, applies error styling
 */
function showToast(message, isError = false) {
    // Create toast if it doesn't exist
    let toast = document.querySelector('.toast-container');
    if (!toast) {
        toast = document.createElement('div');
        toast.className = 'toast-container';
        document.body.appendChild(toast);
    }

    // Apply content and error class
    toast.textContent = message;
    if (isError) {
        toast.classList.add('error');
    } else {
        toast.classList.remove('error');
    }

    // Trigger animation
    setTimeout(() => toast.classList.add('show'), 10);

    // Hide and cleanup
    setTimeout(() => {
        toast.classList.remove('show');
    }, 3000);
}