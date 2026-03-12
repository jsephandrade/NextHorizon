document.addEventListener("DOMContentLoaded", function() {

    // ----------------- TABS -----------------
    function openTab(evt, tabName) {
        document.querySelectorAll(".tab-content").forEach(c => c.classList.remove("active"));
        document.querySelectorAll(".tab").forEach(t => t.classList.remove("active"));

        const target = document.getElementById(tabName);
        if(target) target.classList.add("active");
        evt.currentTarget.classList.add("active");
    }

    // Bind tabs
    document.querySelectorAll(".settings-tabs .tab").forEach(tab => {
        tab.addEventListener("click", (e) => openTab(e, tab.getAttribute("onclick").split("'")[1]));
    });

    // ----------------- LOGO PREVIEW -----------------
    const logoInput = document.getElementById("logoUpload");
    if(logoInput){
        logoInput.addEventListener("change", function(e){
            const file = e.target.files[0];
            if(file){
                const reader = new FileReader();
                reader.onload = function(ev){
                    document.getElementById("logoPreview").src = ev.target.result;
                }
                reader.readAsDataURL(file);
            }
        });
    }

    // ----------------- MODALS -----------------
    function closeAllModals(){
        document.querySelectorAll(".modal-overlay").forEach(modal => modal.style.display = "none");
    }

    function openPasswordModal(){
        closeAllModals();
        const modal = document.getElementById("passwordModal");
        if(modal) modal.style.display = "flex";
    }

    function openUpdateModal(message = "Are you sure you want to save these changes?"){
        const messageElem = document.getElementById("updateModalMessage");
        if(messageElem) messageElem.innerText = message;
        closeAllModals();
        const modal = document.getElementById("updateModal");
        if(modal) modal.style.display = "flex";
    }

    function confirmUpdate(){
        closeAllModals();
        const successModal = document.getElementById("passwordChangedModal");
        if(successModal) successModal.style.display = "flex";
    }

    // Close modal if clicking outside
    window.addEventListener("click", function(event){
        document.querySelectorAll(".modal-overlay").forEach(modal => {
            if(event.target === modal){
                modal.style.display = "none";
            }
        });
    });

    // ----------------- ACCOUNT DETAILS EDIT/VIEW -----------------
    let isEditingAccount = false;

    const accountEditBtn = document.getElementById("accountEditBtn");
    if(accountEditBtn){
        accountEditBtn.addEventListener("click", function() {
            const inputs = document.querySelectorAll("#account input");
            if(!isEditingAccount){
                // Enable all inputs
                inputs.forEach(input => input.removeAttribute("disabled"));
                accountEditBtn.textContent = "Save Account Changes";
                isEditingAccount = true;
            } else {
                // Disable inputs
                inputs.forEach(input => input.setAttribute("disabled", true));
                accountEditBtn.textContent = "Edit Account";
                isEditingAccount = false;

                // Open confirmation modal
                openUpdateModal("Are you sure you want to save these changes?");
            }
        });
    }

    // Confirm button inside modal
    const confirmBtn = document.getElementById("confirmUpdateBtn");
    if(confirmBtn){
        confirmBtn.addEventListener("click", confirmUpdate);
    }

});