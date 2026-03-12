(function () {
    const form = document.getElementById("activityForm");
    const activityNameInput = document.getElementById("activityName");
    const activityDateInput = document.getElementById("activityDate");
    const movingTimeInput = document.getElementById("MovingTime");
    const movingTimeDisplay = document.getElementById("movingTimeDisplay");
    const hourInput = document.getElementById("hourInput");
    const minuteInput = document.getElementById("minuteInput");
    const secondInput = document.getElementById("secondInput");
    const hourValue = document.getElementById("hourValue");
    const minuteValue = document.getElementById("minuteValue");
    const secondValue = document.getElementById("secondValue");
    const timeModal = document.getElementById("timePickerModal");
    const distanceModal = document.getElementById("distancePickerModal");
    const distanceWholeInput = document.getElementById("distanceWhole");
    const distanceDecimalInput = document.getElementById("distanceDecimal");
    const distanceUnitInput = document.getElementById("distanceUnit");
    const distanceWholeValue = document.getElementById("distanceWholeValue");
    const distanceDecimalValue = document.getElementById("distanceDecimalValue");
    const distanceUnitValue = document.getElementById("distanceUnitValue");
    const distanceInput = document.getElementById("DistanceKm");
    const distanceUnitHiddenInput = document.getElementById("DistanceUnit");
    const distanceDisplay = document.getElementById("distanceDisplay");
    const stepsInput = document.getElementById("stepsInput");
    const proofFileInput = document.getElementById("proofFile");
    const previewImage = document.getElementById("previewImage");
    const removeButton = document.getElementById("removeBtn");
    const submitButton = document.getElementById("submitBtn");
    let csrfTokenPromise = null;

    const warningIds = [
        "activityWarning",
        "dateWarning",
        "timeWarning",
        "distanceWarning",
        "stepsWarning",
        "proofWarning",
    ];

    function setWarning(id, message, isError = true) {
        const element = document.getElementById(id);
        if (!element) {
            return;
        }

        element.textContent = message;
        element.style.color = isError ? "#c1121f" : "#2a9d8f";
    }

    async function getCsrfToken() {
        if (!csrfTokenPromise) {
            csrfTokenPromise = fetch("/api/security/csrf-token", {
                method: "GET",
                credentials: "same-origin",
            })
                .then(async (response) => {
                    if (!response.ok) {
                        throw new Error("Unable to initialize secure upload.");
                    }

                    const payload = await response.json();
                    return typeof payload?.token === "string" ? payload.token : "";
                })
                .catch((error) => {
                    csrfTokenPromise = null;
                    throw error;
                });
        }

        return csrfTokenPromise;
    }

    async function readErrorMessage(response) {
        const contentType = response.headers.get("content-type") || "";
        if (contentType.includes("application/json")) {
            const payload = await response.json().catch(() => null);
            const errors = payload?.errors;
            if (errors && typeof errors === "object") {
                const firstGroup = Object.values(errors).find((value) => Array.isArray(value) && value.length > 0);
                if (firstGroup) {
                    return firstGroup[0];
                }
            }

            if (typeof payload?.title === "string" && payload.title.trim()) {
                return payload.title;
            }

            if (typeof payload?.message === "string" && payload.message.trim()) {
                return payload.message;
            }
        }

        const text = await response.text().catch(() => "");
        return text || "Upload failed.";
    }

    function clearWarnings() {
        for (const warningId of warningIds) {
            setWarning(warningId, "");
        }
    }

    function parseNonNegativeInt(value) {
        const parsed = Number.parseInt(value ?? "0", 10);
        return Number.isFinite(parsed) && parsed >= 0 ? parsed : 0;
    }

    function normalizeDistanceUnit(value) {
        return (value || "").toLowerCase() === "mi" ? "mi" : "km";
    }

    function parseTwoDigitDecimal(value) {
        const digitsOnly = String(value ?? "").replace(/\D/g, "").slice(0, 2);
        if (!digitsOnly) {
            return 0;
        }

        const parsed = Number.parseInt(digitsOnly, 10);
        return Number.isFinite(parsed) ? Math.min(parsed, 99) : 0;
    }

    function clearDefaultFieldValue(input, defaultValue) {
        if (!input) {
            return;
        }

        if (input.value === defaultValue) {
            input.value = "";
        }
    }

    function getCurrentDistance() {
        const whole = parseNonNegativeInt(distanceWholeInput?.value);
        const decimal = parseTwoDigitDecimal(distanceDecimalInput?.value);
        return whole + (decimal / 100);
    }

    function hasActiveModal() {
        return Boolean(document.querySelector(".time-modal.active"));
    }

    function hideModal(modal) {
        if (!modal) {
            return;
        }

        modal.classList.remove("active");
        modal.setAttribute("aria-hidden", "true");
        if (!hasActiveModal()) {
            document.body.classList.remove("modal-open");
        }
    }

    function showModal(modal) {
        if (!modal) {
            return;
        }

        modal.classList.add("active");
        modal.setAttribute("aria-hidden", "false");
        document.body.classList.add("modal-open");
    }

    function syncTimePreview({ normalizeInputs = false } = {}) {
        const hours = Math.min(parseNonNegativeInt(hourInput?.value), 23);
        const minutes = Math.min(parseNonNegativeInt(minuteInput?.value), 59);
        const seconds = Math.min(parseNonNegativeInt(secondInput?.value), 59);

        if (normalizeInputs && hourInput) {
            hourInput.value = hours.toString();
        }

        if (normalizeInputs && minuteInput) {
            minuteInput.value = minutes.toString();
        }

        if (normalizeInputs && secondInput) {
            secondInput.value = seconds.toString();
        }

        if (hourValue) {
            hourValue.textContent = hours.toString().padStart(2, "0");
        }

        if (minuteValue) {
            minuteValue.textContent = minutes.toString().padStart(2, "0");
        }

        if (secondValue) {
            secondValue.textContent = seconds.toString().padStart(2, "0");
        }
    }

    function loadTimePicker() {
        const committedSeconds = parseNonNegativeInt(movingTimeInput?.value);
        const hours = Math.min(Math.floor(committedSeconds / 3600), 23);
        const minutes = Math.min(Math.floor((committedSeconds % 3600) / 60), 59);
        const seconds = Math.min(committedSeconds % 60, 59);

        if (hourInput) {
            hourInput.value = hours.toString();
        }

        if (minuteInput) {
            minuteInput.value = minutes.toString();
        }

        if (secondInput) {
            secondInput.value = seconds.toString();
        }

        syncTimePreview({ normalizeInputs: true });
    }

    function syncDistancePreview({ normalizeInputs = false } = {}) {
        const whole = parseNonNegativeInt(distanceWholeInput?.value);
        const decimal = parseTwoDigitDecimal(distanceDecimalInput?.value);
        const unit = normalizeDistanceUnit(distanceUnitInput?.value);

        if (normalizeInputs && distanceWholeInput) {
            distanceWholeInput.value = whole.toString();
        }

        if (normalizeInputs && distanceDecimalInput) {
            distanceDecimalInput.value = decimal.toString().padStart(2, "0");
        }

        if (distanceUnitInput) {
            distanceUnitInput.value = unit;
        }

        if (distanceWholeValue) {
            distanceWholeValue.textContent = whole.toString();
        }

        if (distanceDecimalValue) {
            distanceDecimalValue.textContent = decimal.toString().padStart(2, "0");
        }

        if (distanceUnitValue) {
            distanceUnitValue.textContent = unit;
        }
    }

    function loadDistancePicker() {
        const committedDistance = Number.parseFloat(distanceInput?.value ?? "");
        const committedUnit = normalizeDistanceUnit(distanceUnitHiddenInput?.value);

        if (Number.isFinite(committedDistance) && committedDistance > 0) {
            const whole = Math.floor(committedDistance);
            const decimal = Math.round((committedDistance - whole) * 100);

            if (distanceWholeInput) {
                distanceWholeInput.value = whole.toString();
            }

            if (distanceDecimalInput) {
                distanceDecimalInput.value = decimal.toString().padStart(2, "0");
            }

            if (distanceUnitInput) {
                distanceUnitInput.value = committedUnit;
            }
        } else {
            if (distanceWholeInput) {
                distanceWholeInput.value = "0";
            }

            if (distanceDecimalInput) {
                distanceDecimalInput.value = "00";
            }

            if (distanceUnitInput) {
                distanceUnitInput.value = "km";
            }
        }

        syncDistancePreview({ normalizeInputs: true });
    }

    function resetTimePicker() {
        if (hourInput) {
            hourInput.value = "0";
        }

        if (minuteInput) {
            minuteInput.value = "0";
        }

        if (secondInput) {
            secondInput.value = "0";
        }

        if (movingTimeInput) {
            movingTimeInput.value = "";
        }

        if (movingTimeDisplay) {
            movingTimeDisplay.value = "";
        }

        syncTimePreview({ normalizeInputs: true });
    }

    function resetDistancePicker() {
        if (distanceWholeInput) {
            distanceWholeInput.value = "0";
        }

        if (distanceDecimalInput) {
            distanceDecimalInput.value = "00";
        }

        if (distanceUnitInput) {
            distanceUnitInput.value = "km";
        }

        if (distanceInput) {
            distanceInput.value = "";
        }

        if (distanceUnitHiddenInput) {
            distanceUnitHiddenInput.value = "km";
        }

        if (distanceDisplay) {
            distanceDisplay.value = "";
        }

        syncDistancePreview({ normalizeInputs: true });
    }

    function setPreviewImage(file) {
        if (!previewImage || !removeButton) {
            return;
        }

        const previousObjectUrl = previewImage.dataset.objectUrl;
        if (previousObjectUrl) {
            URL.revokeObjectURL(previousObjectUrl);
            delete previewImage.dataset.objectUrl;
        }

        if (!file) {
            previewImage.removeAttribute("src");
            previewImage.style.display = "none";
            previewImage.classList.remove("loaded");
            removeButton.classList.remove("visible");
            removeButton.style.display = "none";
            return;
        }

        const objectUrl = URL.createObjectURL(file);
        previewImage.src = objectUrl;
        previewImage.dataset.objectUrl = objectUrl;
        previewImage.style.display = "block";
        previewImage.classList.add("loaded");
        removeButton.classList.add("visible");
        removeButton.style.display = "inline-flex";
    }

    function validateForm() {
        clearWarnings();
        let isValid = true;

        const activityName = activityNameInput?.value?.trim() ?? "";
        if (!activityName) {
            setWarning("activityWarning", "Please select an activity.");
            isValid = false;
        }

        const activityDate = activityDateInput?.value?.trim() ?? "";
        if (!activityDate) {
            setWarning("dateWarning", "Please pick an activity date.");
            isValid = false;
        }

        const movingTimeSec = parseNonNegativeInt(movingTimeInput?.value);
        if (movingTimeSec <= 0) {
            setWarning("timeWarning", "Moving time must be greater than zero.");
            isValid = false;
        }

        const distance = Number.parseFloat(distanceInput?.value ?? "0");
        if (!Number.isFinite(distance) || distance <= 0) {
            setWarning("distanceWarning", "Distance must be greater than zero.");
            isValid = false;
        }

        const steps = stepsInput?.value?.trim();
        if (steps) {
            const parsedSteps = Number.parseInt(steps, 10);
            if (!Number.isFinite(parsedSteps) || parsedSteps <= 0) {
                setWarning("stepsWarning", "Steps must be a positive whole number.");
                isValid = false;
            }
        }

        if (!proofFileInput?.files || proofFileInput.files.length === 0) {
            setWarning("proofWarning", "Proof image is required.");
            isValid = false;
        }

        return isValid;
    }

    function buildGeneratedTitle(activityName, activityDate) {
        const normalizedActivity = activityName.trim();
        const normalizedDate = activityDate.trim();
        return `${normalizedActivity} - ${normalizedDate}`;
    }

    async function submitForm(event) {
        event.preventDefault();
        if (!validateForm()) {
            return;
        }

        const proofFile = proofFileInput.files[0];
        const movingTimeSec = parseNonNegativeInt(movingTimeInput.value);
        const distance = Number.parseFloat(distanceInput.value);
        const distanceUnit = (distanceUnitHiddenInput?.value || "km").toLowerCase();
        const activityName = activityNameInput.value.trim();
        const activityDate = activityDateInput.value;
        const title = buildGeneratedTitle(activityName, activityDate);

        const payload = new FormData();
        payload.append("Title", title);
        payload.append("ActivityName", activityName);
        payload.append("ActivityDate", activityDate);
        payload.append("MovingTimeSec", movingTimeSec.toString());
        if (stepsInput?.value?.trim()) {
            payload.append("Steps", stepsInput.value.trim());
        }

        if (distanceUnit === "mi") {
            payload.append("DistanceMi", distance.toFixed(2));
        } else {
            payload.append("DistanceKm", distance.toFixed(2));
        }

        payload.append("Proof", proofFile);

        submitButton.disabled = true;
        submitButton.textContent = "Submitting...";
        try {
            const csrfToken = await getCsrfToken();
            const response = await fetch("/api/uploads", {
                method: "POST",
                headers: csrfToken ? { "X-CSRF-TOKEN": csrfToken } : undefined,
                body: payload,
                credentials: "same-origin",
            });

            if (!response.ok) {
                const message = await readErrorMessage(response);
                setWarning("activityWarning", message || "Upload failed.");
                return;
            }

            window.location.assign("/Member/MyActivity");
        } catch (_error) {
            setWarning("activityWarning", "Unable to submit activity right now.");
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = "Submit Activity";
        }
    }

    window.openTimePicker = function openTimePicker() {
        loadTimePicker();
        showModal(timeModal);
        hourInput?.focus();
    };

    window.applyTime = function applyTime() {
        const hours = Math.min(parseNonNegativeInt(hourInput?.value), 23);
        const minutes = Math.min(parseNonNegativeInt(minuteInput?.value), 59);
        const seconds = Math.min(parseNonNegativeInt(secondInput?.value), 59);
        const totalSeconds = (hours * 3600) + (minutes * 60) + seconds;
        movingTimeInput.value = totalSeconds.toString();
        movingTimeDisplay.value = `${hours.toString().padStart(2, "0")}:${minutes
            .toString()
            .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
        syncTimePreview({ normalizeInputs: true });
        hideModal(timeModal);
    };

    window.openDistancePicker = function openDistancePicker() {
        loadDistancePicker();
        showModal(distanceModal);
        distanceWholeInput?.focus();
    };

    window.applyDistance = function applyDistance() {
        const distance = getCurrentDistance();
        const unit = normalizeDistanceUnit(distanceUnitInput?.value);
        if (distance <= 0) {
            setWarning("distanceWarning", "Distance must be greater than zero.");
            syncDistancePreview({ normalizeInputs: true });
            distanceWholeInput?.focus();
            return;
        }

        distanceInput.value = distance.toFixed(2);
        distanceUnitHiddenInput.value = unit;
        distanceDisplay.value = `${distance.toFixed(2)} ${unit}`;
        setWarning("distanceWarning", "");
        syncDistancePreview({ normalizeInputs: true });
        hideModal(distanceModal);
    };

    window.handleFileSelect = function handleFileSelect(event) {
        const file = event?.target?.files?.[0] || null;
        setPreviewImage(file);
    };

    window.removeImage = function removeImage() {
        if (!proofFileInput) {
            return;
        }

        proofFileInput.value = "";
        setPreviewImage(null);
    };

    if (timeModal) {
        timeModal.addEventListener("click", (event) => {
            if (event.target === timeModal) {
                hideModal(timeModal);
            }
        });
    }

    if (distanceModal) {
        distanceModal.addEventListener("click", (event) => {
            if (event.target === distanceModal) {
                hideModal(distanceModal);
            }
        });
    }

    document.addEventListener("keydown", (event) => {
        if (event.key !== "Escape") {
            return;
        }

        if (timeModal?.classList.contains("active")) {
            hideModal(timeModal);
            return;
        }

        if (distanceModal?.classList.contains("active")) {
            hideModal(distanceModal);
        }
    });

    if (form) {
        form.addEventListener("submit", submitForm);
    }

    hourInput?.addEventListener("input", () => {
        syncTimePreview();
    });
    hourInput?.addEventListener("focus", () => {
        clearDefaultFieldValue(hourInput, "0");
    });
    hourInput?.addEventListener("blur", () => {
        syncTimePreview({ normalizeInputs: true });
    });
    hourInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            window.applyTime();
        }
    });
    minuteInput?.addEventListener("input", () => {
        syncTimePreview();
    });
    minuteInput?.addEventListener("focus", () => {
        clearDefaultFieldValue(minuteInput, "0");
    });
    minuteInput?.addEventListener("blur", () => {
        syncTimePreview({ normalizeInputs: true });
    });
    minuteInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            window.applyTime();
        }
    });
    secondInput?.addEventListener("input", () => {
        syncTimePreview();
    });
    secondInput?.addEventListener("focus", () => {
        clearDefaultFieldValue(secondInput, "0");
    });
    secondInput?.addEventListener("blur", () => {
        syncTimePreview({ normalizeInputs: true });
    });
    secondInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            window.applyTime();
        }
    });
    distanceWholeInput?.addEventListener("input", () => {
        syncDistancePreview();
    });
    distanceWholeInput?.addEventListener("focus", () => {
        clearDefaultFieldValue(distanceWholeInput, "0");
    });
    distanceWholeInput?.addEventListener("blur", () => {
        syncDistancePreview({ normalizeInputs: true });
    });
    distanceWholeInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            window.applyDistance();
        }
    });
    distanceDecimalInput?.addEventListener("input", () => {
        if (distanceDecimalInput) {
            distanceDecimalInput.value = String(distanceDecimalInput.value ?? "").replace(/\D/g, "").slice(0, 2);
        }

        syncDistancePreview();
    });
    distanceDecimalInput?.addEventListener("focus", () => {
        clearDefaultFieldValue(distanceDecimalInput, "00");
    });
    distanceDecimalInput?.addEventListener("blur", () => {
        syncDistancePreview({ normalizeInputs: true });
    });
    distanceDecimalInput?.addEventListener("keydown", (event) => {
        if (event.key === "Enter") {
            event.preventDefault();
            window.applyDistance();
        }
    });
    distanceUnitInput?.addEventListener("change", () => {
        syncDistancePreview({ normalizeInputs: true });
    });

    resetTimePicker();
    resetDistancePicker();
    setPreviewImage(null);
})();
