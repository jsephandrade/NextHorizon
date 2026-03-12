(function () {
    const SKELETON_CARD_COUNT = 6;
    const EMPTY_ACTIVITY_MESSAGE = 'No activities yet. <a href="/Member/UploadActivity">Upload activity first</a>.';
    const FILTER_LABELS = {
        all: "all dates",
        today: "today",
        thisWeek: "this week",
        thisMonth: "this month",
        last7Days: "the last 7 days",
        last30Days: "the last 30 days",
    };

    const sortSelect = document.getElementById("sortSelect");
    const dateFilterSelect = document.getElementById("dateFilterSelect");
    const activityGrid = document.getElementById("activityGrid");
    const activityStatus = document.getElementById("activityStatus");
    const modal = document.getElementById("activity-modal");
    const modalImage = document.getElementById("modal-activity-img");
    const modalFrame = modal?.querySelector(".activity-modal-frame");
    const modalEmptyState = document.getElementById("modal-activity-empty");
    const modalTitle = document.getElementById("modal-activity-title");
    const modalType = document.getElementById("modal-activity-type");
    const modalDate = document.getElementById("modal-activity-date");
    const modalTime = document.getElementById("modal-activity-time");
    const modalDistance = document.getElementById("modal-activity-distance");
    const modalPace = document.getElementById("modal-activity-pace");
    const modalSteps = document.getElementById("modal-activity-steps");
    const dateFormatter = new Intl.DateTimeFormat(undefined, {
        month: "short",
        day: "numeric",
        year: "numeric",
    });
    const renderedItemsById = new Map();
    const cardTemplate = document.createElement("template");
    const skeletonTemplate = document.createElement("template");

    let cachedItems = [];

    function setModalOpen(isOpen) {
        if (!modal) {
            return;
        }

        modal.style.display = isOpen ? "block" : "none";
        document.body.classList.toggle("activity-modal-open", isOpen);
    }

    cardTemplate.innerHTML = `
        <article class="activity-card" tabindex="0">
            <div class="activity-card__media">
                <img class="activity-img" alt="" />
                <span class="activity-card__badge" data-slot="badge"></span>
            </div>
            <div class="activity-card__body">
                <div class="activity-card__header">
                    <h3 class="activity-card__title" data-slot="title"></h3>
                    <span class="activity-card__date" data-slot="date"></span>
                </div>
                <p class="activity-card__subline" data-slot="subline"></p>
                <div class="activity-card__metrics">
                    <div class="metric-chip">
                        <span class="metric-chip__label">Distance</span>
                        <span class="metric-chip__value" data-slot="metric-distance"></span>
                    </div>
                    <div class="metric-chip">
                        <span class="metric-chip__label">Time</span>
                        <span class="metric-chip__value" data-slot="metric-time"></span>
                    </div>
                    <div class="metric-chip">
                        <span class="metric-chip__label">Avg Pace</span>
                        <span class="metric-chip__value" data-slot="metric-pace"></span>
                    </div>
                </div>
            </div>
        </article>`;

    skeletonTemplate.innerHTML = `
        <article class="activity-card skeleton-card" aria-hidden="true">
            <div class="activity-card__media skeleton-media skeleton-block"></div>
            <div class="activity-card__body">
                <div class="activity-card__header">
                    <div class="skeleton-title-group">
                        <div class="skeleton-title skeleton-block"></div>
                        <div class="skeleton-title-short skeleton-block"></div>
                    </div>
                    <div class="skeleton-date skeleton-block"></div>
                </div>
                <div class="skeleton-subline skeleton-block"></div>
                <div class="activity-card__metrics">
                    <div class="metric-chip">
                        <div class="skeleton-metric-label skeleton-block"></div>
                        <div class="skeleton-metric-value skeleton-block"></div>
                    </div>
                    <div class="metric-chip">
                        <div class="skeleton-metric-label skeleton-block"></div>
                        <div class="skeleton-metric-value skeleton-block"></div>
                    </div>
                    <div class="metric-chip">
                        <div class="skeleton-metric-label skeleton-block"></div>
                        <div class="skeleton-metric-value skeleton-block"></div>
                    </div>
                </div>
                <span class="skeleton-sr-only">Loading activities</span>
            </div>
        </article>`;

    function getValue(item, camelCaseName, pascalCaseName) {
        if (!item || typeof item !== "object") {
            return undefined;
        }

        if (camelCaseName in item) {
            return item[camelCaseName];
        }

        if (pascalCaseName in item) {
            return item[pascalCaseName];
        }

        return undefined;
    }

    function setStatus(message, isError = false, allowHtml = false) {
        if (!activityStatus) {
            return;
        }

        activityStatus.style.display = message ? "block" : "none";
        if (allowHtml) {
            activityStatus.innerHTML = message;
        } else {
            activityStatus.textContent = message;
        }

        activityStatus.classList.remove("status-info", "status-success", "status-error");
        if (message) {
            activityStatus.classList.add(isError ? "status-error" : "status-info");
        }
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }

        const parsed = value instanceof Date ? value : new Date(value);
        if (Number.isNaN(parsed.getTime())) {
            return "-";
        }

        return dateFormatter.format(parsed);
    }

    function formatTime(seconds) {
        if (!Number.isFinite(seconds) || seconds <= 0) {
            return "-";
        }

        const total = Math.floor(seconds);
        const h = Math.floor(total / 3600);
        const m = Math.floor((total % 3600) / 60);
        const s = total % 60;
        if (h > 0) {
            return `${h}h ${m}m ${s}s`;
        }

        return `${m}m ${s}s`;
    }

    function formatPace(seconds) {
        if (!Number.isFinite(seconds) || seconds <= 0) {
            return "-";
        }

        const total = Math.floor(seconds);
        const mins = Math.floor(total / 60);
        const secs = total % 60;
        return `${mins}:${secs.toString().padStart(2, "0")} /km`;
    }

    function parseDateOnly(value) {
        if (!value || typeof value !== "string") {
            return null;
        }

        const trimmed = value.length >= 10 ? value.slice(0, 10) : value;
        const parsed = new Date(`${trimmed}T00:00:00`);
        return Number.isNaN(parsed.getTime()) ? null : parsed;
    }

    function formatDistancePair(distanceKm, distanceMi) {
        return `${Number.isFinite(distanceKm) ? distanceKm.toFixed(2) : "-"} km (${Number.isFinite(distanceMi) ? distanceMi.toFixed(2) : "-"} mi)`;
    }

    function normalizeItem(item) {
        const activityDateRaw = getValue(item, "activityDate", "ActivityDate");
        const activityDate = parseDateOnly(typeof activityDateRaw === "string" ? activityDateRaw : "");
        const distanceKm = Number(getValue(item, "distanceKm", "DistanceKm"));
        const distanceMi = Number(getValue(item, "distanceMi", "DistanceMi"));
        const movingTimeSec = Number(getValue(item, "movingTimeSec", "MovingTimeSec"));
        const avgPaceSecPerKm = Number(getValue(item, "avgPaceSecPerKm", "AvgPaceSecPerKm"));

        return {
            uploadId: String(getValue(item, "uploadId", "UploadId") ?? ""),
            proofUrl: getValue(item, "proofUrl", "ProofUrl") || "",
            title: getValue(item, "title", "Title") || "Untitled",
            activityName: getValue(item, "activityName", "ActivityName") || "-",
            activityDate,
            activityDateLabel: formatDate(activityDateRaw),
            movingTimeLabel: formatTime(movingTimeSec),
            distanceSummary: formatDistancePair(distanceKm, distanceMi),
            distanceLabel: Number.isFinite(distanceKm) ? `${distanceKm.toFixed(2)} km` : "-",
            avgPaceLabel: formatPace(avgPaceSecPerKm),
            steps: getValue(item, "steps", "Steps"),
        };
    }

    function passDateFilter(item, filter) {
        if (!filter || filter === "all") {
            return true;
        }

        if (!item.activityDate) {
            return false;
        }

        const now = new Date();
        const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());

        if (filter === "today") {
            return item.activityDate.getTime() === today.getTime();
        }

        if (filter === "thisWeek") {
            const day = today.getDay();
            const weekStart = new Date(today);
            weekStart.setDate(today.getDate() - day);
            return item.activityDate >= weekStart && item.activityDate <= today;
        }

        if (filter === "thisMonth") {
            return item.activityDate.getMonth() === today.getMonth()
                && item.activityDate.getFullYear() === today.getFullYear();
        }

        if (filter === "last7Days") {
            const start = new Date(today);
            start.setDate(today.getDate() - 6);
            return item.activityDate >= start && item.activityDate <= today;
        }

        if (filter === "last30Days") {
            const start = new Date(today);
            start.setDate(today.getDate() - 29);
            return item.activityDate >= start && item.activityDate <= today;
        }

        return true;
    }

    function renderModal(item) {
        if (modalFrame) {
            modalFrame.classList.remove("is-empty", "is-loading");
        }

        if (modalImage) {
            if (item.proofUrl) {
                if (modalFrame) {
                    modalFrame.classList.add("is-loading");
                }

                modalImage.onload = function handleModalImageLoad() {
                    modalFrame?.classList.remove("is-loading");
                };

                modalImage.onerror = function handleModalImageError() {
                    modalImage.removeAttribute("src");
                    modalImage.classList.add("hidden");
                    modalFrame?.classList.remove("is-loading");
                    modalFrame?.classList.add("is-empty");
                    modalEmptyState?.classList.remove("hidden");
                };

                modalImage.src = item.proofUrl;
                modalImage.classList.remove("hidden");

                if (modalImage.complete && modalImage.naturalWidth > 0) {
                    modalFrame?.classList.remove("is-loading");
                }
            } else {
                modalImage.removeAttribute("src");
                modalImage.classList.add("hidden");
                modalImage.onload = null;
                modalImage.onerror = null;
                modalFrame?.classList.add("is-empty");
            }

            modalImage.alt = item.title;
        }

        if (modalEmptyState) {
            modalEmptyState.classList.toggle("hidden", Boolean(item.proofUrl));
        }

        if (modalTitle) {
            modalTitle.textContent = item.title;
        }

        if (modalType) {
            modalType.textContent = item.activityName;
        }

        if (modalDate) {
            modalDate.textContent = item.activityDateLabel;
        }

        if (modalTime) {
            modalTime.textContent = item.movingTimeLabel;
        }

        if (modalDistance) {
            modalDistance.textContent = item.distanceSummary;
        }

        if (modalPace) {
            modalPace.textContent = item.avgPaceLabel;
        }

        if (modalSteps) {
            modalSteps.textContent = item.steps ?? "-";
        }
    }

    function buildActivityCard(item) {
        const card = cardTemplate.content.firstElementChild.cloneNode(true);
        const image = card.querySelector(".activity-img");

        card.dataset.uploadId = item.uploadId;

        if (image) {
            if (item.proofUrl) {
                image.src = item.proofUrl;
            } else {
                image.removeAttribute("src");
            }

            image.alt = item.title;
            image.loading = "lazy";
            image.decoding = "async";
            image.fetchPriority = "low";
        }

        const badge = card.querySelector('[data-slot="badge"]');
        const title = card.querySelector('[data-slot="title"]');
        const date = card.querySelector('[data-slot="date"]');
        const subline = card.querySelector('[data-slot="subline"]');
        const metricDistance = card.querySelector('[data-slot="metric-distance"]');
        const metricTime = card.querySelector('[data-slot="metric-time"]');
        const metricPace = card.querySelector('[data-slot="metric-pace"]');

        if (badge) {
            badge.textContent = item.activityName;
        }

        if (title) {
            title.textContent = item.title;
        }

        if (date) {
            date.textContent = item.activityDateLabel;
        }

        if (subline) {
            subline.textContent = item.distanceSummary;
        }

        if (metricDistance) {
            metricDistance.textContent = item.distanceLabel;
        }

        if (metricTime) {
            metricTime.textContent = item.movingTimeLabel;
        }

        if (metricPace) {
            metricPace.textContent = item.avgPaceLabel;
        }

        return card;
    }

    function renderItems(items) {
        if (!activityGrid) {
            return;
        }

        renderedItemsById.clear();

        const fragment = document.createDocumentFragment();
        for (const item of items) {
            if (item.uploadId) {
                renderedItemsById.set(item.uploadId, item);
            }

            fragment.appendChild(buildActivityCard(item));
        }

        activityGrid.replaceChildren(fragment);
    }

    function renderSkeletonCards() {
        if (!activityGrid) {
            return;
        }

        renderedItemsById.clear();

        const fragment = document.createDocumentFragment();
        for (let index = 0; index < SKELETON_CARD_COUNT; index += 1) {
            fragment.appendChild(skeletonTemplate.content.firstElementChild.cloneNode(true));
        }

        activityGrid.replaceChildren(fragment);
    }

    function setLoading(isLoading) {
        if (!activityGrid) {
            return;
        }

        activityGrid.setAttribute("aria-busy", isLoading ? "true" : "false");
        activityGrid.dataset.loading = isLoading ? "true" : "false";

        if (isLoading) {
            setStatus("");
            renderSkeletonCards();
        }
    }

    function getActiveFilter() {
        return dateFilterSelect?.value || "all";
    }

    function getFilteredEmptyMessage(filter) {
        return `No activities found for ${FILTER_LABELS[filter] || "the selected date range"}.`;
    }

    function applyFilters(items) {
        const activeFilter = getActiveFilter();
        return items.filter((item) => passDateFilter(item, activeFilter));
    }

    function renderCurrentView() {
        if (!activityGrid) {
            return;
        }

        if (!cachedItems.length) {
            renderedItemsById.clear();
            activityGrid.replaceChildren();
            setStatus(EMPTY_ACTIVITY_MESSAGE, false, true);
            return;
        }

        const filteredItems = applyFilters(cachedItems);
        if (!filteredItems.length) {
            renderedItemsById.clear();
            activityGrid.replaceChildren();
            setStatus(getFilteredEmptyMessage(getActiveFilter()));
            return;
        }

        setStatus("");
        renderItems(filteredItems);
    }

    async function fetchActivities(sort) {
        const response = await fetch(`/api/uploads/my?sort=${encodeURIComponent(sort)}&page=1&pageSize=100`, {
            method: "GET",
            credentials: "same-origin",
        });

        if (!response.ok) {
            throw new Error((await response.text()) || "Unable to load activities.");
        }

        const payload = await response.json();
        const items = payload.items || payload.Items || [];
        return items.map(normalizeItem);
    }

    async function loadActivities() {
        if (!activityGrid) {
            return;
        }

        setLoading(true);

        try {
            const sort = sortSelect?.value || "createdAt_desc";
            cachedItems = await fetchActivities(sort);
            setLoading(false);
            renderCurrentView();
        } catch (error) {
            cachedItems = [];
            renderedItemsById.clear();
            setLoading(false);
            activityGrid.replaceChildren();
            setStatus(error instanceof Error ? error.message : "Unable to load activities right now.", true);
        }
    }

    function openActivityFromCard(target) {
        if (!activityGrid || activityGrid.dataset.loading === "true") {
            return;
        }

        if (!(target instanceof Element)) {
            return;
        }

        const card = target.closest(".activity-card[data-upload-id]");
        if (!card) {
            return;
        }

        const item = renderedItemsById.get(card.dataset.uploadId || "");
        if (!item) {
            return;
        }

        renderModal(item);
        setModalOpen(true);
    }

    window.closeActivityModal = function closeActivityModal() {
        setModalOpen(false);
    };

    if (modal) {
        modal.addEventListener("click", (event) => {
            if (event.target === modal) {
                window.closeActivityModal();
            }
        });
    }

    activityGrid?.addEventListener("click", (event) => {
        openActivityFromCard(event.target);
    });

    activityGrid?.addEventListener("keydown", (event) => {
        if (event.key !== "Enter" && event.key !== " ") {
            return;
        }

        event.preventDefault();
        openActivityFromCard(event.target);
    });

    sortSelect?.addEventListener("change", loadActivities);
    dateFilterSelect?.addEventListener("change", renderCurrentView);

    loadActivities();
})();
