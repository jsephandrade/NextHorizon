
document.addEventListener('DOMContentLoaded', async function () {
    // 1. Element Selectors
    const regionInput = document.getElementById('regionInput');
    const regionList = document.getElementById('regionList');
    const provinceInput = document.getElementById('provinceInput');
    const provinceList = document.getElementById('provinceList');
    const cityInput = document.getElementById('cityInput');
    const cityList = document.getElementById('cityList');
    const barangayInput = document.getElementById('barangayInput');
    const barangayList = document.getElementById('barangayList');
    const addressId = document.getElementById('addressId');

    const houseInput = document.getElementById('houseInput');
    const buildingInput = document.getElementById('buildingInput');
    const streetInput = document.getElementById('streetNameInput');
    const postalInput = document.getElementById('postalInput');
    const defaultToggle = document.getElementById('defaultToggle');

    const addressList = document.getElementById('addressList');
    const emptyState = document.getElementById('emptyState');
    const showFormBtn = document.getElementById('showFormBtn');
    const emptyAddBtn = document.getElementById('emptyAddBtn');
    const newAddressForm = document.getElementById('newAddressForm');
    const saveBtn = document.getElementById('saveBtn');
    const cancelBtn = document.getElementById('cancelBtn');
    const formCancelBtn = document.getElementById('formCancelBtn');
    const shippingForm = document.getElementById('shippingAddressForm');

    let addressData = {};
    let editingId = null;

    // Monochrome Swal Config
    const swalConfig = {
        confirmButtonColor: '#000000',
        iconColor: '#000000',
        background: '#ffffff',
        color: '#000000',
        customClass: {
            confirmButton: 'btn-pill btn-black',
            cancelButton: 'btn-pill btn-gray'
        }
    };

    const regionNames = {
        "01": "Region I (Ilocos Region)", 
        "02": "Region II (Cagayan Valley)",
        "03": "Region III (Central Luzon)", 
        "4A": "Region IV-A (CALABARZON)",
        "4B": "Region IV-B (MIMAROPA)", 
        "05": "Region V (Bicol Region)",
        "06": "Region VI (Western Visayas)", 
        "07": "Region VII (Central Visayas)",
        "08": "Region VIII (Eastern Visayas)", 
        "09": "Region IX (Zamboanga Peninsula)",
        "10": "Region X (Northern Mindanao)", 
        "11": "Region XI (Davao Region)",
        "12": "Region XII (SOCCSKSARGEN)", 
        "13": "Region XIII (Caraga)",
        "ARMM": "ARMM", 
        "CAR": "CAR", 
        "NCR": "NCR (Metro Manila)", 
        "NIR": "NIR (Negros Island Region)"
    };

    // --- 2. Load Data ---
    async function loadData() {
        try {
            const response = await fetch('/philippine_provinces_cities_municipalities_and_barangays_2016.json');
            if (!response.ok) throw new Error("Geographic data file not found");
            addressData = await response.json();
            
            console.log("Data loaded successfully");
            console.log("Available regions:", Object.keys(addressData));
            
            populateDatalist(regionList, Object.keys(addressData), regionNames);
        } catch (e) {
            console.error("Error loading geographic data:", e);
        }
    }

    // --- 3. Clear lists when clicking on inputs ---
    regionInput.addEventListener('click', function() {
        provinceList.innerHTML = '';
        cityList.innerHTML = '';
        barangayList.innerHTML = '';
    });

    provinceInput.addEventListener('click', function() {
        cityList.innerHTML = '';
        barangayList.innerHTML = '';
    });

    cityInput.addEventListener('click', function() {
        barangayList.innerHTML = '';
    });

    // --- 4. Cascading Datalist Logic ---
    regionInput.addEventListener('input', function () {
        const code = getCodeByValue(this.value, regionNames);
        provinceInput.value = ''; 
        cityInput.value = ''; 
        barangayInput.value = '';
        
        // Clear dependent datalists
        provinceList.innerHTML = '';
        cityList.innerHTML = '';
        barangayList.innerHTML = '';
        
        if (addressData[code] && addressData[code].province_list) {
            const provinces = Object.keys(addressData[code].province_list);
            populateDatalist(provinceList, provinces);
            console.log("Provinces loaded:", provinces);
        }
    });

    provinceInput.addEventListener('input', function () {
        const regCode = getCodeByValue(regionInput.value, regionNames);
        cityInput.value = ''; 
        barangayInput.value = '';
        
        // Clear dependent datalists
        cityList.innerHTML = '';
        barangayList.innerHTML = '';
        
        if (addressData[regCode] && 
            addressData[regCode].province_list && 
            addressData[regCode].province_list[this.value]) {
            
            const municipalityObj = addressData[regCode].province_list[this.value].municipality_list;
            if (municipalityObj) {
                const cities = Object.keys(municipalityObj);
                populateDatalist(cityList, cities);
                console.log("Cities loaded:", cities);
            }
        }
    });

    cityInput.addEventListener('input', function () {
        const regCode = getCodeByValue(regionInput.value, regionNames);
        const provName = provinceInput.value;
        barangayInput.value = '';
        
        // Clear barangay datalist
        barangayList.innerHTML = '';
        
        if (addressData[regCode] && 
            addressData[regCode].province_list && 
            addressData[regCode].province_list[provName] && 
            addressData[regCode].province_list[provName].municipality_list &&
            addressData[regCode].province_list[provName].municipality_list[this.value]) {
            
            const barangays = addressData[regCode].province_list[provName].municipality_list[this.value].barangay_list;
            if (barangays && Array.isArray(barangays)) {
                populateDatalist(barangayList, barangays);
                console.log("Barangays loaded:", barangays);
            }
        }
    });

    // --- 5. Helpers ---
    function populateDatalist(listElement, items, mapping = null) {
        if (!items || !Array.isArray(items)) return;
        
        listElement.innerHTML = '';
        items.sort().forEach(item => {
            const option = document.createElement('option');
            option.value = mapping && mapping[item] ? mapping[item] : item;
            listElement.appendChild(option);
        });
    }

    function getCodeByValue(val, mapping) {
        if (!val) return val;
        // First check if the value is already a code
        if (mapping[val]) return val;
        // Otherwise find by name
        return Object.keys(mapping).find(key => mapping[key] === val) || val;
    }

    function checkEmptyState() {
        const cards = addressList.querySelectorAll('.saved-address-card');
        const isEmpty = cards.length === 0;

        emptyState.style.display = isEmpty ? 'block' : 'none';
        addressList.style.display = isEmpty ? 'none' : 'grid';
        showFormBtn.style.display = isEmpty ? 'none' : 'block';
    }

    function resetForm() {
        shippingForm.reset();
        addressId.value = '';
        editingId = null;
        document.querySelectorAll('.custom-input').forEach(i => i.style.borderColor = '#e0e0e0');
        
        // Clear all datalists except regions
        provinceList.innerHTML = '';
        cityList.innerHTML = '';
        barangayList.innerHTML = '';
    }

    function openForm() {
        newAddressForm.style.display = 'block';
        showFormBtn.style.display = 'none';
        emptyState.style.display = 'none';
        newAddressForm.scrollIntoView({ behavior: 'smooth' });
    }

    function closeForm() {
        newAddressForm.style.display = 'none';
        resetForm();
        checkEmptyState();
    }

    // --- 6. Event Listeners for Toggles ---
    if (showFormBtn) showFormBtn.addEventListener('click', openForm);
    if (emptyAddBtn) emptyAddBtn.addEventListener('click', openForm);
    if (cancelBtn) cancelBtn.addEventListener('click', closeForm);
    if (formCancelBtn) formCancelBtn.addEventListener('click', closeForm);

    // --- 7. Save Logic with Backend ---
    shippingForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const required = [regionInput, provinceInput, cityInput, barangayInput, houseInput, streetInput, postalInput];
        let isValid = true;

        required.forEach(input => {
            if (!input.value.trim()) {
                input.style.borderColor = '#000000';
                isValid = false;
            } else {
                input.style.borderColor = '#e0e0e0';
            }
        });

        if (!isValid) {
            Swal.fire({
                ...swalConfig,
                title: 'REQUIRED FIELDS',
                text: 'Please complete all highlighted fields.',
                icon: 'warning'
            });
            return;
        }

        const formData = new FormData(shippingForm);
        
        // Add anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }
        
        try {
            const response = await fetch('/AccountProfile/SaveShippingAddress', {
                method: 'POST',
                body: formData,
                headers: {
                    'RequestVerificationToken': token
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                Swal.fire({
                    ...swalConfig,
                    title: editingId ? 'UPDATED' : 'SUCCESS',
                    text: result.message,
                    icon: 'success',
                    timer: 1800,
                    showConfirmButton: false
                });

                setTimeout(() => window.location.reload(), 1800);
            } else {
                Swal.fire({
                    ...swalConfig,
                    title: 'ERROR',
                    text: result.message,
                    icon: 'error'
                });
            }
        } catch (error) {
            console.error('Fetch error:', error);
            Swal.fire({
                ...swalConfig,
                title: 'ERROR',
                text: 'An error occurred while saving. Please try again.',
                icon: 'error'
            });
        }
    });

    // --- 8. Edit Action ---
    function setupEditButton(editBtn, card) {
        editBtn.addEventListener('click', function(e) {
            e.stopPropagation(); // Prevent triggering default address
            
            editingId = card.dataset.id;
            addressId.value = card.dataset.id;
            
            // Set form values
            regionInput.value = card.dataset.region;
            provinceInput.value = card.dataset.province;
            cityInput.value = card.dataset.city;
            barangayInput.value = card.dataset.barangay;
            houseInput.value = card.dataset.house;
            buildingInput.value = card.dataset.building || '';
            streetInput.value = card.dataset.street;
            postalInput.value = card.dataset.postal;
            defaultToggle.checked = card.dataset.isdefault === 'true';

            // Trigger cascading updates to populate datalists
            regionInput.dispatchEvent(new Event('input'));
            
            // Small delay to allow region data to load before setting province
            setTimeout(() => {
                provinceInput.dispatchEvent(new Event('input'));
            }, 100);
            
            setTimeout(() => {
                cityInput.dispatchEvent(new Event('input'));
            }, 200);

            openForm();
        });
    }

    // --- 9. Delete Action ---
    function setupDeleteButton(deleteBtn, card) {
        deleteBtn.addEventListener('click', async function(e) {
            e.stopPropagation(); // Prevent triggering default address
            
            const addressId = card.dataset.id;
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            const result = await Swal.fire({
                ...swalConfig,
                title: 'REMOVE ADDRESS?',
                text: "This will permanently delete this shipping location.",
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'CONFIRM DELETE',
                cancelButtonText: 'CANCEL'
            });

            if (result.isConfirmed) {
                try {
                    const formData = new FormData();
                    formData.append('id', addressId);
                    if (token) {
                        formData.append('__RequestVerificationToken', token);
                    }

                    const response = await fetch('/AccountProfile/DeleteShippingAddress', {
                        method: 'POST',
                        body: formData
                    });

                    const data = await response.json();

                    if (data.success) {
                        card.remove();
                        checkEmptyState();
                        Swal.fire({ 
                            ...swalConfig, 
                            title: 'REMOVED', 
                            icon: 'success', 
                            timer: 1200, 
                            showConfirmButton: false 
                        });
                    } else {
                        Swal.fire({
                            ...swalConfig,
                            title: 'ERROR',
                            text: data.message,
                            icon: 'error'
                        });
                    }
                } catch (error) {
                    console.error('Delete error:', error);
                    Swal.fire({
                        ...swalConfig,
                        title: 'ERROR',
                        text: 'An error occurred while deleting.',
                        icon: 'error'
                    });
                }
            }
        });
    }

    // --- 10. Set Default Action ---
    function setupDefaultClick(card) {
        card.addEventListener('click', async function(e) {
            // Don't trigger if clicking on edit or delete buttons
            if (e.target.closest('.edit-btn') || e.target.closest('.delete-btn')) {
                return;
            }

            const addrId = this.dataset.id;
            const wasDefault = this.classList.contains('default-card');
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            // If it's already default, show message and return
            if (wasDefault) {
                Swal.fire({
                    ...swalConfig,
                    title: 'ALREADY DEFAULT',
                    text: 'This is already your default address.',
                    icon: 'info',
                    timer: 1500,
                    showConfirmButton: false
                });
                return;
            }
            
            // Show confirmation dialog
            const result = await Swal.fire({
                ...swalConfig,
                title: 'SET AS DEFAULT?',
                text: "Do you want to set this as your default shipping address?",
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'YES, SET DEFAULT',
                cancelButtonText: 'CANCEL'
            });

            if (!result.isConfirmed) return;
            
            try {
                const formData = new FormData();
                formData.append('id', addrId);
                if (token) {
                    formData.append('__RequestVerificationToken', token);
                }

                const response = await fetch('/AccountProfile/SetDefaultShippingAddress', {
                    method: 'POST',
                    body: formData
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const data = await response.json();

                if (data.success) {
                    // Show success message and reload
                    Swal.fire({
                        ...swalConfig,
                        title: 'SUCCESS',
                        text: 'Default address updated. Page will reload.',
                        icon: 'success',
                        timer: 1500,
                        showConfirmButton: false
                    });
                    
                    // Reload the page to reflect changes
                    setTimeout(() => window.location.reload(), 1500);
                } else {
                    Swal.fire({
                        ...swalConfig,
                        title: 'ERROR',
                        text: data.message || 'Failed to set default address.',
                        icon: 'error'
                    });
                }
            } catch (error) {
                console.error('Error setting default address:', error);
                Swal.fire({
                    ...swalConfig,
                    title: 'ERROR',
                    text: 'An error occurred while setting default address.',
                    icon: 'error'
                });
            }
        });
    }

    // --- 11. Initialize Cards ---
    function initializeCards() {
        const cards = addressList.querySelectorAll('.saved-address-card');
        
        cards.forEach(card => {
            const editBtn = card.querySelector('.edit-btn');
            const deleteBtn = card.querySelector('.delete-btn');
            
            if (editBtn) setupEditButton(editBtn, card);
            if (deleteBtn) setupDeleteButton(deleteBtn, card);
            
            // Setup default click
            setupDefaultClick(card);
        });
    }

    // Initialize cards that are already in the DOM
    initializeCards();

    // Observer for dynamically added cards (if any)
    const observer = new MutationObserver(function(mutations) {
        mutations.forEach(function(mutation) {
            if (mutation.addedNodes.length) {
                initializeCards();
            }
        });
    });

    observer.observe(addressList, { childList: true, subtree: true });

    // Initialize
    await loadData();
    checkEmptyState();
});
