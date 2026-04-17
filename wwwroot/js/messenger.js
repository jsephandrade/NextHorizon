(function () {
    const config = window.messengerPageConfig || window.sellerMessengerConfig;
    if (!config) {
        return;
    }

    const POLL_INTERVAL_MS = 5000;
    let csrfTokenPromise = null;

    const root = document.querySelector("[data-messenger-root]") || document.querySelector("[data-seller-messenger-root]");
    if (!root) {
        return;
    }

    const elements = {
        list: root.querySelector("[data-conversations-list]"),
        search: root.querySelector("[data-conversation-search]"),
        noResults: root.querySelector("[data-no-results]"),
        status: root.querySelector("[data-messenger-status]"),
        title: root.querySelector("[data-chat-title]"),
        subtitle: root.querySelector("[data-chat-subtitle]"),
        avatar: root.querySelector("[data-chat-avatar]"),
        messages: root.querySelector("[data-messages-container]"),
        input: root.querySelector("[data-message-input]"),
        attachmentInput: root.querySelector("[data-message-attachment-input]"),
        attachmentButton: root.querySelector("[data-attach-message]"),
        composerAttachmentPreview: root.querySelector("[data-composer-attachment-preview]"),
        send: root.querySelector("[data-send-message]"),
        refresh: root.querySelector("[data-refresh-conversations]")
    };

    const state = {
        conversations: [],
        activeConversationId: null,
        currentUserId: String(config.currentUserId || ""),
        pollHandle: null,
        search: "",
        hasLoadedConversations: false,
        isRefreshingConversations: false,
        conversationListSignature: "",
        renderedConversationId: null,
        messagesByConversationId: {},
        messageSignatures: {},
        messageRequestId: 0,
        imageViewer: null,
        selectedAttachment: null,
        selectedAttachmentPreviewUrl: null,
        videoFpsByUrl: {},
        videoFpsPromises: {}
    };

    function messagingRole() {
        return config.messagingRole === "consumer" ? "consumer" : "seller";
    }

    function expectedCounterpartyRole() {
        return String(config.expectedCounterpartyRole || (messagingRole() === "consumer" ? "seller" : "consumer")).toLowerCase();
    }

    function buildHeaders(includeJson) {
        const headers = {};
        if (includeJson) {
            headers["Content-Type"] = "application/json";
        }

        if (config.apiMode === "main" && config.debugUserId) {
            headers["X-Debug-UserId"] = String(config.debugUserId);
        }

        return headers;
    }

    function buildUrl(path, query) {
        const url = new URL(path, window.location.origin);
        Object.keys(query || {}).forEach(function (key) {
            const value = query[key];
            if (value === null || value === undefined || value === "") {
                return;
            }

            url.searchParams.set(key, String(value));
        });

        return url.toString();
    }

    async function parsePayload(response) {
        const text = await response.text();
        if (!text) {
            return null;
        }

        try {
            return JSON.parse(text);
        } catch (_) {
            return text;
        }
    }

    function friendlyError(response, payload) {
        if (response.status === 401 || response.status === 403) {
            return messagingRole() === "seller"
                ? "This seller session cannot access messaging."
                : "This session cannot access messaging.";
        }

        if (response.status === 404) {
            return "Conversation data is not available for this mode.";
        }

        if (typeof payload === "string" && payload.trim()) {
            return payload;
        }

        if (typeof payload?.title === "string" && payload.title.trim()) {
            return payload.title;
        }

        return "Messaging request failed.";
    }

    async function getCsrfToken() {
        if (!csrfTokenPromise) {
            csrfTokenPromise = fetch("/api/security/csrf-token", {
                credentials: "same-origin",
                headers: buildHeaders(false)
            })
                .then(async function (response) {
                    const payload = await parsePayload(response);
                    if (!response.ok) {
                        throw new Error(friendlyError(response, payload));
                    }

                    return typeof payload?.token === "string" ? payload.token : "";
                })
                .catch(function (error) {
                    csrfTokenPromise = null;
                    throw error;
                });
        }

        return csrfTokenPromise;
    }

    async function request(path, options, query) {
        const response = await fetch(buildUrl(path, query), Object.assign({
            credentials: "same-origin"
        }, options || {}));

        const payload = await parsePayload(response);
        if (!response.ok) {
            throw new Error(friendlyError(response, payload));
        }

        return payload;
    }

    async function requestWithCsrf(path, options, query) {
        const token = await getCsrfToken();
        const headers = Object.assign({}, options?.headers || {});
        if (token) {
            headers["X-CSRF-TOKEN"] = token;
        }

        return request(path, Object.assign({}, options || {}, { headers: headers }), query);
    }

    function formatTime(value) {
        if (!value) {
            return "";
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? ""
            : date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
    }

    function formatDateTime(value) {
        if (!value) {
            return "";
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? ""
            : date.toLocaleString([], { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" });
    }

    function normalizeConversation(item) {
        return {
            conversationId: Number(item.conversationId || item.ConversationId || 0),
            currentUserId: String(item.currentUserId || item.CurrentUserId || ""),
            buyerUserId: String(item.buyerUserId || item.BuyerUserId || ""),
            sellerUserId: String(item.sellerUserId || item.SellerUserId || ""),
            displayName: String(item.displayName || item.DisplayName || ""),
            displaySubtitle: String(item.displaySubtitle || item.DisplaySubtitle || ""),
            avatarUrl: String(item.avatarUrl || item.AvatarUrl || ""),
            contextLabel: String(item.contextLabel || item.ContextLabel || ""),
            counterpartyRole: String(item.counterpartyRole || item.CounterpartyRole || ""),
            counterpartyId: String(item.counterpartyId || item.CounterpartyId || ""),
            canReply: item.canReply !== undefined ? Boolean(item.canReply) : Boolean(item.CanReply),
            orderId: item.orderId || item.OrderId || null,
            lastMessagePreview: String(item.lastMessagePreview || item.LastMessagePreview || ""),
            unreadCount: Number(item.unreadCount || item.UnreadCount || 0),
            updatedAt: item.updatedAt || item.UpdatedAt || item.lastMessageAt || item.LastMessageAt || null
        };
    }

    function filterConversationsByRole(conversations) {
        if (config.apiMode !== "main") {
            return conversations;
        }

        return conversations.filter(function (conversation) {
            return !expectedCounterpartyRole() || conversation.counterpartyRole === expectedCounterpartyRole();
        });
    }

    function normalizeMessage(item) {
        return {
            messageId: String(item.messageId || item.MessageId || ""),
            senderUserId: String(item.senderUserId || item.SenderUserId || ""),
            body: String(item.body || item.Body || ""),
            attachmentUrl: String(item.attachmentUrl || item.AttachmentUrl || ""),
            sentAt: item.sentAt || item.SentAt || null,
            isDeleted: Boolean(item.isDeleted || item.IsDeleted)
        };
    }

    function composerAttachmentKind(file) {
        if (!file) {
            return "file";
        }

        const fileName = String(file.name || "");
        const extension = attachmentExtension(fileName);
        if (["jpg", "jpeg", "png", "webp"].includes(extension) || String(file.type || "").startsWith("image/")) {
            return "image";
        }

        if (["mp4", "webm", "mov"].includes(extension) || String(file.type || "").startsWith("video/")) {
            return "video";
        }

        return "file";
    }

    function formatBytes(bytes) {
        const size = Number(bytes || 0);
        if (size <= 0) {
            return "";
        }

        if (size < 1024 * 1024) {
            return Math.round(size / 1024) + " KB";
        }

        return (size / (1024 * 1024)).toFixed(1) + " MB";
    }

    function attachmentExtension(url) {
        if (!url) {
            return "";
        }

        try {
            const parsed = new URL(url, window.location.origin);
            const pathname = parsed.pathname || "";
            const lastSegment = pathname.split("/").pop() || "";
            const lastDot = lastSegment.lastIndexOf(".");
            return lastDot >= 0
                ? lastSegment.slice(lastDot + 1).toLowerCase()
                : "";
        } catch (_) {
            const sanitized = String(url).split("#")[0].split("?")[0];
            const lastSegment = sanitized.split("/").pop() || "";
            const lastDot = lastSegment.lastIndexOf(".");
            return lastDot >= 0
                ? lastSegment.slice(lastDot + 1).toLowerCase()
                : "";
        }
    }

    function attachmentKind(url) {
        const extension = attachmentExtension(url);
        if (["jpg", "jpeg", "png", "webp"].includes(extension)) {
            return "image";
        }

        if (["mp4", "webm", "mov"].includes(extension)) {
            return "video";
        }

        return "file";
    }

    function createAttachmentLink(url, label) {
        const link = document.createElement("a");
        link.href = url;
        link.className = "attachment-link";
        link.textContent = label;
        return link;
    }

    function createAttachmentMeta(value) {
        const meta = document.createElement("span");
        meta.className = "attachment-meta";
        meta.textContent = value;
        return meta;
    }

    function formatFps(fps) {
        if (!Number.isFinite(fps) || fps <= 0) {
            return "";
        }

        const rounded = Math.round(fps * 100) / 100;
        return rounded.toFixed(2).replace(/\.?0+$/, "") + " FPS";
    }

    function measureVideoFps(url) {
        if (!url) {
            return Promise.resolve(null);
        }

        if (Object.prototype.hasOwnProperty.call(state.videoFpsByUrl, url)) {
            return Promise.resolve(state.videoFpsByUrl[url]);
        }

        if (state.videoFpsPromises[url]) {
            return state.videoFpsPromises[url];
        }

        state.videoFpsPromises[url] = new Promise(function (resolve) {
            const probe = document.createElement("video");
            const sampleWindowSeconds = 1;
            let frameCount = 0;
            let startMediaTime = null;
            let lastMediaTime = null;
            let finished = false;
            let timeoutHandle = 0;

            function cleanup(result) {
                if (finished) {
                    return;
                }

                finished = true;
                if (timeoutHandle) {
                    window.clearTimeout(timeoutHandle);
                }

                probe.pause();
                probe.removeAttribute("src");
                probe.load();

                if (probe.parentNode) {
                    probe.parentNode.removeChild(probe);
                }

                state.videoFpsByUrl[url] = result;
                delete state.videoFpsPromises[url];
                resolve(result);
            }

            if (typeof probe.requestVideoFrameCallback !== "function") {
                cleanup(null);
                return;
            }

            probe.muted = true;
            probe.defaultMuted = true;
            probe.playsInline = true;
            probe.preload = "auto";
            probe.style.position = "fixed";
            probe.style.left = "-9999px";
            probe.style.top = "0";
            probe.style.width = "1px";
            probe.style.height = "1px";
            probe.style.opacity = "0";
            probe.style.pointerEvents = "none";
            probe.src = url;

            function onFrame(_, metadata) {
                if (finished) {
                    return;
                }

                const mediaTime = Number(metadata?.mediaTime);
                if (!Number.isFinite(mediaTime)) {
                    probe.requestVideoFrameCallback(onFrame);
                    return;
                }

                if (startMediaTime === null) {
                    startMediaTime = mediaTime;
                }

                lastMediaTime = mediaTime;
                frameCount += 1;

                if ((lastMediaTime - startMediaTime) >= sampleWindowSeconds && frameCount > 1) {
                    const fps = (frameCount - 1) / (lastMediaTime - startMediaTime);
                    cleanup(Number.isFinite(fps) && fps > 0 ? fps : null);
                    return;
                }

                probe.requestVideoFrameCallback(onFrame);
            }

            probe.addEventListener("loadeddata", function () {
                if (finished) {
                    return;
                }

                probe.requestVideoFrameCallback(onFrame);

                const playResult = probe.play();
                if (playResult && typeof playResult.catch === "function") {
                    playResult.catch(function () {
                        cleanup(null);
                    });
                }
            }, { once: true });

            probe.addEventListener("error", function () {
                cleanup(null);
            }, { once: true });

            timeoutHandle = window.setTimeout(function () {
                cleanup(null);
            }, 6000);

            document.body.appendChild(probe);
        });

        return state.videoFpsPromises[url];
    }

    function hydrateVideoFps(url, label) {
        if (!label || !url) {
            return;
        }

        measureVideoFps(url).then(function (fps) {
            if (!label.isConnected) {
                return;
            }

            const value = formatFps(fps);
            if (!value) {
                label.remove();
                return;
            }

            label.textContent = value;
            label.hidden = false;
            scrollMessagesToLatest();
        });
    }

    function hasMessageText(message) {
        return !message.isDeleted && Boolean((message.body || "").trim());
    }

    function isAttachmentOnlyMessage(message) {
        return !message.isDeleted && Boolean(message.attachmentUrl) && !hasMessageText(message);
    }

    function ensureImageViewer() {
        if (state.imageViewer) {
            return state.imageViewer;
        }

        const overlay = document.createElement("div");
        overlay.className = "image-viewer";
        overlay.hidden = true;

        const backdrop = document.createElement("button");
        backdrop.type = "button";
        backdrop.className = "image-viewer-backdrop";
        backdrop.setAttribute("aria-label", "Close image preview");

        const stage = document.createElement("div");
        stage.className = "image-viewer-stage";
        stage.setAttribute("role", "dialog");
        stage.setAttribute("aria-modal", "true");
        stage.setAttribute("aria-label", "Image preview");

        const close = document.createElement("button");
        close.type = "button";
        close.className = "image-viewer-close";
        close.setAttribute("aria-label", "Close image preview");
        close.textContent = "\u00D7";

        const image = document.createElement("img");
        image.className = "image-viewer-image";
        image.alt = "Expanded attachment preview";

        const error = document.createElement("p");
        error.className = "image-viewer-error";
        error.hidden = true;
        error.textContent = "Image preview is not available.";

        const fallback = document.createElement("a");
        fallback.className = "image-viewer-fallback";
        fallback.hidden = true;
        fallback.textContent = "Open image file";
        fallback.target = "_blank";
        fallback.rel = "noopener noreferrer";

        function resetViewerState() {
            image.hidden = true;
            image.removeAttribute("src");
            error.hidden = true;
            fallback.hidden = true;
            fallback.removeAttribute("href");
        }

        function closeViewer() {
            overlay.hidden = true;
            document.body.classList.remove("messenger-image-viewer-open");
            resetViewerState();
        }

        function showViewerError(url) {
            image.hidden = true;
            error.hidden = false;
            fallback.hidden = false;
            fallback.href = url;
        }

        close.addEventListener("click", closeViewer);
        backdrop.addEventListener("click", closeViewer);
        image.addEventListener("click", closeViewer);
        fallback.addEventListener("click", function () {
            closeViewer();
        });

        image.addEventListener("load", function () {
            image.hidden = false;
            error.hidden = true;
            fallback.hidden = true;
            fallback.removeAttribute("href");
        });

        image.addEventListener("error", function () {
            const url = image.getAttribute("src") || "";
            showViewerError(url);
        });

        stage.appendChild(close);
        stage.appendChild(image);
        stage.appendChild(error);
        stage.appendChild(fallback);
        overlay.appendChild(backdrop);
        overlay.appendChild(stage);
        document.body.appendChild(overlay);

        document.addEventListener("keydown", function (event) {
            if (event.key === "Escape" && !overlay.hidden) {
                closeViewer();
            }
        });

        state.imageViewer = {
            overlay: overlay,
            image: image,
            fallback: fallback,
            open: function (url) {
                resetViewerState();
                image.hidden = true;
                image.src = url;
                overlay.hidden = false;
                document.body.classList.add("messenger-image-viewer-open");
            },
            close: closeViewer
        };

        return state.imageViewer;
    }

    function openImageViewer(url) {
        if (!url) {
            return;
        }

        ensureImageViewer().open(url);
    }

    function renderAttachmentPreview(message) {
        if (message.isDeleted || !message.attachmentUrl) {
            return null;
        }

        const kind = attachmentKind(message.attachmentUrl);
        const container = document.createElement("div");
        container.className = "message-attachment";

        if (kind === "image") {
            const previewButton = document.createElement("button");
            previewButton.type = "button";
            previewButton.className = "attachment-preview-button";
            previewButton.setAttribute("aria-label", "Preview image attachment");
            previewButton.addEventListener("click", function () {
                openImageViewer(message.attachmentUrl);
            });

            const image = document.createElement("img");
            image.src = message.attachmentUrl;
            image.alt = "Attachment preview";
            image.className = "attachment-preview attachment-image";
            image.loading = "lazy";

            previewButton.appendChild(image);
            container.appendChild(previewButton);
            return container;
        }

        if (kind === "video") {
            const video = document.createElement("video");
            video.className = "attachment-preview attachment-video";
            video.controls = true;
            video.preload = "metadata";
            video.playsInline = true;
            video.src = message.attachmentUrl;

            const metaRow = document.createElement("div");
            metaRow.className = "attachment-meta-row";

            const fpsLabel = createAttachmentMeta("");
            fpsLabel.hidden = true;
            metaRow.appendChild(fpsLabel);
            metaRow.appendChild(createAttachmentLink(message.attachmentUrl, "Open video"));

            container.appendChild(video);
            container.appendChild(metaRow);
            hydrateVideoFps(message.attachmentUrl, fpsLabel);
            return container;
        }

        container.appendChild(createAttachmentLink(message.attachmentUrl, "Open attachment"));
        return container;
    }

    function setStatus(message, isError) {
        if (!message || !isError) {
            elements.status.hidden = true;
            elements.status.textContent = "";
            elements.status.classList.remove("is-error");
            return;
        }

        elements.status.hidden = false;
        elements.status.textContent = message;
        elements.status.classList.toggle("is-error", Boolean(isError));
    }

    function clearComposerAttachment() {
        if (state.selectedAttachmentPreviewUrl) {
            URL.revokeObjectURL(state.selectedAttachmentPreviewUrl);
        }

        state.selectedAttachment = null;
        state.selectedAttachmentPreviewUrl = null;
        elements.attachmentInput.value = "";
        elements.composerAttachmentPreview.hidden = true;
        elements.composerAttachmentPreview.innerHTML = "";
    }

    function renderComposerAttachmentPreview() {
        const file = state.selectedAttachment;
        const preview = elements.composerAttachmentPreview;
        preview.innerHTML = "";

        if (!file) {
            preview.hidden = true;
            return;
        }

        const kind = composerAttachmentKind(file);
        const card = document.createElement("div");
        card.className = "composer-attachment-card";

        if (kind === "image" && state.selectedAttachmentPreviewUrl) {
            const image = document.createElement("img");
            image.src = state.selectedAttachmentPreviewUrl;
            image.alt = file.name || "Selected attachment";
            image.className = "composer-attachment-media composer-attachment-image";
            card.appendChild(image);
        } else if (kind === "video" && state.selectedAttachmentPreviewUrl) {
            const video = document.createElement("video");
            video.src = state.selectedAttachmentPreviewUrl;
            video.className = "composer-attachment-media composer-attachment-video";
            video.preload = "metadata";
            video.muted = true;
            video.playsInline = true;
            card.appendChild(video);
        } else {
            const icon = document.createElement("div");
            icon.className = "composer-attachment-file-icon";
            icon.innerHTML = '<i class="fas fa-file"></i>';
            card.appendChild(icon);
        }

        const meta = document.createElement("div");
        meta.className = "composer-attachment-meta";

        const name = document.createElement("div");
        name.className = "composer-attachment-name";
        name.textContent = file.name || "Attachment";

        const details = document.createElement("div");
        details.className = "composer-attachment-details";
        details.textContent = formatBytes(file.size);

        meta.appendChild(name);
        meta.appendChild(details);

        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "composer-attachment-remove";
        remove.setAttribute("aria-label", "Remove attachment");
        remove.innerHTML = '<i class="fas fa-times"></i>';
        remove.addEventListener("click", clearComposerAttachment);

        card.appendChild(meta);
        card.appendChild(remove);

        preview.appendChild(card);
        preview.hidden = false;
    }

    function setComposerEnabled(enabled) {
        elements.input.disabled = !enabled;
        elements.send.disabled = !enabled;
        elements.attachmentButton.disabled = !enabled;
        elements.attachmentInput.disabled = !enabled;
        elements.input.placeholder = "Type a message...";
    }

    function conversationTitle(conversation) {
        if (conversation.displayName) {
            return conversation.displayName;
        }

        if (conversation.counterpartyRole === "seller") {
            return "Seller";
        }

        if (conversation.counterpartyRole === "consumer") {
            return "Consumer";
        }

        return "Conversation";
    }

    function conversationSubtitle(conversation) {
        if (conversation.displaySubtitle) {
            return conversation.displaySubtitle;
        }

        if (conversation.contextLabel) {
            return conversation.contextLabel;
        }

        return "";
    }

    function initialsFromConversation(conversation) {
        const source = conversation.displayName || conversation.counterpartyId || conversation.buyerUserId || "?";
        const tokens = source
            .trim()
            .split(/\s+/)
            .filter(Boolean);

        if (tokens.length >= 2) {
            return (tokens[0][0] + tokens[1][0]).toUpperCase();
        }

        return source.slice(0, 2).toUpperCase();
    }

    function applyInitialSelection(conversations) {
        if (!conversations.length) {
            state.activeConversationId = null;
            return;
        }

        const preferred = conversations.find(function (conversation) {
            if (config.initialConversationId && conversation.conversationId === Number(config.initialConversationId)) {
                return true;
            }

            if (config.orderId && Number(conversation.orderId) === Number(config.orderId)) {
                return true;
            }

            if (config.consumerId && Number(conversation.buyerUserId) === Number(config.consumerId)) {
                return true;
            }

            if (config.sellerId && Number(conversation.sellerUserId) === Number(config.sellerId)) {
                return true;
            }

            if (config.sellerId && Number(conversation.counterpartyId) === Number(config.sellerId)) {
                return true;
            }

            return false;
        });

        state.activeConversationId = (preferred || conversations[0]).conversationId;
    }

    function filteredConversations() {
        const value = state.search.trim().toLowerCase();
        if (!value) {
            return state.conversations;
        }

        return state.conversations.filter(function (conversation) {
            const haystack = [
                conversationTitle(conversation),
                conversation.lastMessagePreview || "",
                conversation.orderId ? "order " + conversation.orderId : ""
            ].join(" ").toLowerCase();

            return haystack.includes(value);
        });
    }

    function conversationSignature(conversations) {
        return conversations.map(function (conversation) {
            return [
                conversation.conversationId,
                conversation.currentUserId,
                conversation.buyerUserId,
                conversation.sellerUserId,
                conversation.orderId,
                conversation.lastMessagePreview,
                conversation.unreadCount,
                conversation.updatedAt
            ].join("|");
        }).join("||");
    }

    function messageSignature(messages) {
        return messages.map(function (message) {
            return [
                message.messageId,
                message.senderUserId,
                message.body,
                message.attachmentUrl,
                message.sentAt,
                message.isDeleted ? "1" : "0"
            ].join("|");
        }).join("||");
    }

    function getActiveConversation() {
        return state.conversations.find(function (item) {
            return item.conversationId === state.activeConversationId;
        }) || null;
    }

    function syncConversationHeader(conversation) {
        if (!conversation) {
            elements.title.textContent = "Conversation";
            elements.subtitle.textContent = "";
            elements.avatar.textContent = "--";
            setComposerEnabled(false);
            return;
        }

        elements.title.textContent = conversationTitle(conversation);
        elements.subtitle.textContent = conversationSubtitle(conversation);
        elements.avatar.textContent = initialsFromConversation(conversation);
        state.currentUserId = conversation.currentUserId || state.currentUserId;
        setComposerEnabled(conversation.canReply !== false);
    }

    function renderConversations() {
        const conversations = filteredConversations();
        elements.list.innerHTML = "";

        if (!conversations.length) {
            elements.noResults.hidden = false;
            return;
        }

        elements.noResults.hidden = true;

        conversations.forEach(function (conversation) {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "conversation-item" + (conversation.conversationId === state.activeConversationId ? " active" : "");

            const initials = initialsFromConversation(conversation);
            item.innerHTML =
                '<div class="conversation-avatar"><div class="avatar-placeholder">' + initials + '</div></div>' +
                '<div class="conversation-info">' +
                '<div class="conversation-name">' + conversationTitle(conversation) + "</div>" +
                '<div class="conversation-meta">' +
                '<span class="last-message">' + (conversation.lastMessagePreview || "No messages yet.") + "</span>" +
                (conversation.unreadCount > 0 ? '<span class="unread-badge">' + conversation.unreadCount + "</span>" : "") +
                "</div>" +
                '<div class="conversation-time">' + formatDateTime(conversation.updatedAt) + "</div>" +
                "</div>";

            item.addEventListener("click", function () {
                if (state.activeConversationId === conversation.conversationId) {
                    return;
                }

                state.activeConversationId = conversation.conversationId;
                renderConversations();
                syncConversationHeader(conversation);
                loadMessages({ showLoading: true, forceRender: true, markAsRead: true });
            });

            elements.list.appendChild(item);
        });
    }

    function renderEmptyMessages(message) {
        elements.messages.innerHTML = "";
        const empty = document.createElement("div");
        empty.className = "empty-state";
        empty.textContent = message;
        elements.messages.appendChild(empty);
    }

    function scrollMessagesToLatest() {
        const container = elements.messages;
        if (!container) {
            return;
        }

        const alignToBottom = function () {
            container.scrollTop = container.scrollHeight;
        };

        alignToBottom();
        window.requestAnimationFrame(alignToBottom);
        window.setTimeout(alignToBottom, 0);
    }

    function renderMessages(messages) {
        elements.messages.innerHTML = "";

        if (!messages.length) {
            renderEmptyMessages("");
            return;
        }

        messages.forEach(function (message) {
            const wrapper = document.createElement("div");
            const isSent = message.senderUserId === state.currentUserId;
            const mediaOnly = isAttachmentOnlyMessage(message);
            wrapper.className = "message " + (isSent ? "sent" : "received");
            if (mediaOnly) {
                wrapper.classList.add("message--media-only");
            }

            const content = document.createElement("div");
            content.className = "message-content";
            if (mediaOnly) {
                content.classList.add("message-content--media-only");
            }

            if (message.isDeleted || message.body) {
                const paragraph = document.createElement("p");
                paragraph.textContent = message.isDeleted ? "[message deleted]" : message.body;
                content.appendChild(paragraph);
            }

            const attachment = renderAttachmentPreview(message);
            if (attachment) {
                attachment.querySelectorAll("img, video").forEach(function (media) {
                    const eventName = media.tagName === "VIDEO" ? "loadedmetadata" : "load";
                    media.addEventListener(eventName, scrollMessagesToLatest, { once: true });
                });
                content.appendChild(attachment);
            }

            const time = document.createElement("span");
            time.className = "message-time";
            time.textContent = formatTime(message.sentAt);

            wrapper.appendChild(content);
            wrapper.appendChild(time);
            elements.messages.appendChild(wrapper);
        });

        scrollMessagesToLatest();
    }

    function renderConversationPlaceholder() {
        syncConversationHeader(null);
        renderEmptyMessages("");
    }

    async function listConversations() {
        if (config.apiMode === "dev") {
            const payload = await request(
                "/api/dev/messages/conversations",
                { headers: buildHeaders(false) },
                { actorUserId: config.actorUserId, page: 1, pageSize: 100 });

            return Array.isArray(payload?.items) ? payload.items.map(normalizeConversation) : [];
        }

        const payload = await request(
            "/api/messages/conversations",
            { headers: buildHeaders(false) },
            { role: messagingRole(), page: 1, pageSize: 100 });

        const conversations = Array.isArray(payload?.items) ? payload.items.map(normalizeConversation) : [];
        return filterConversationsByRole(conversations);
    }

    async function listMessages(conversationId) {
        if (config.apiMode === "dev") {
            const payload = await request(
                "/api/dev/messages/conversations/" + conversationId + "/messages",
                { headers: buildHeaders(false) },
                { actorUserId: config.actorUserId, pageSize: 100 });

            return Array.isArray(payload) ? payload.map(normalizeMessage) : [];
        }

        const payload = await request(
            "/api/messages/conversations/" + conversationId + "/messages",
            { headers: buildHeaders(false) },
            { role: messagingRole(), pageSize: 100 });

        return Array.isArray(payload) ? payload.map(normalizeMessage) : [];
    }

    async function markRead(conversationId) {
        if (config.apiMode === "dev") {
            await request(
                "/api/dev/messages/conversations/" + conversationId + "/read",
                {
                    method: "POST",
                    headers: buildHeaders(true),
                    body: JSON.stringify({ actorUserId: String(config.actorUserId || "") })
                });
            return;
        }

        await requestWithCsrf(
            "/api/messages/conversations/" + conversationId + "/read",
            {
                method: "POST",
                headers: buildHeaders(true),
                body: "{}"
            },
            { role: messagingRole() });
    }

    function validateComposerAttachment(file) {
        if (!file) {
            return "";
        }

        const kind = composerAttachmentKind(file);
        if (kind === "file") {
            return "Attachment must be a valid image or video (jpg, jpeg, png, webp, mp4, webm, mov) and 5MB or smaller.";
        }

        if (Number(file.size || 0) > 5 * 1024 * 1024) {
            return "Attachment must be a valid image or video (jpg, jpeg, png, webp, mp4, webm, mov) and 5MB or smaller.";
        }

        return "";
    }

    async function sendMessage(conversationId, body, attachment) {
        const form = new FormData();
        form.append("body", body);
        if (attachment) {
            form.append("attachment", attachment);
        }

        if (config.apiMode === "dev") {
            form.append("actorUserId", String(config.actorUserId || ""));
            await request(
                "/api/dev/messages/conversations/" + conversationId + "/messages",
                {
                    method: "POST",
                    headers: buildHeaders(false),
                    body: form
                });
            return;
        }

        await requestWithCsrf(
            "/api/messages/conversations/" + conversationId + "/messages",
            {
                method: "POST",
                headers: buildHeaders(false),
                body: form
            },
            { role: messagingRole() });
    }

    async function refreshConversations(options) {
        const settings = Object.assign({
            silent: false,
            forceMessageReload: false,
            preserveStatus: false
        }, options || {});

        if (state.isRefreshingConversations) {
            return;
        }

        state.isRefreshingConversations = true;

        if (!settings.silent) {
            setStatus("", false);
        }

        try {
            const conversations = await listConversations();
            const previousSignature = state.conversationListSignature;
            const previousActiveConversation = getActiveConversation();
            const previousActiveSignature = previousActiveConversation
                ? conversationSignature([previousActiveConversation])
                : "";
            const previousActiveConversationId = state.activeConversationId;

            state.conversations = conversations;
            state.conversationListSignature = conversationSignature(conversations);

            if (!state.activeConversationId || !conversations.some(function (item) { return item.conversationId === state.activeConversationId; })) {
                applyInitialSelection(conversations);
            }

            if (conversations.length) {
                state.currentUserId = conversations[0].currentUserId || state.currentUserId;
            }

            const activeConversation = getActiveConversation();
            const activeConversationChanged = previousActiveConversationId !== state.activeConversationId;
            const activeConversationSignature = activeConversation
                ? conversationSignature([activeConversation])
                : "";

            if (!state.hasLoadedConversations
                || previousSignature !== state.conversationListSignature
                || activeConversationChanged) {
                renderConversations();
            }

            if (!state.activeConversationId) {
                if (!state.hasLoadedConversations || previousActiveConversationId) {
                    renderConversationPlaceholder();
                }
                if (!settings.preserveStatus) {
                    setStatus("", false);
                }
                state.hasLoadedConversations = true;
                return;
            }

            syncConversationHeader(activeConversation);

            const shouldReloadMessages = settings.forceMessageReload
                || !state.messagesByConversationId[state.activeConversationId]
                || activeConversationChanged
                || previousActiveSignature !== activeConversationSignature;

            if (shouldReloadMessages) {
                await loadMessages({
                    showLoading: !settings.silent && !state.messagesByConversationId[state.activeConversationId],
                    forceRender: activeConversationChanged || settings.forceMessageReload,
                    markAsRead: activeConversationChanged
                });
            } else {
                scrollMessagesToLatest();
            }

            if (!settings.preserveStatus) {
                setStatus("", false);
            }
            state.hasLoadedConversations = true;
        } catch (error) {
            if (!state.hasLoadedConversations) {
                state.conversations = [];
                renderConversations();
                renderEmptyMessages(error.message || "Failed to load conversations.");
                syncConversationHeader(null);
            }
            setStatus(error.message || "Failed to load conversations.", true);
        } finally {
            state.isRefreshingConversations = false;
        }
    }

    async function loadMessages(options) {
        const settings = Object.assign({
            showLoading: false,
            forceRender: false,
            markAsRead: false
        }, options || {});

        const conversation = getActiveConversation();

        if (!conversation) {
            renderEmptyMessages("");
            setComposerEnabled(false);
            return;
        }

        syncConversationHeader(conversation);

        if (settings.showLoading && !state.messagesByConversationId[conversation.conversationId]) {
            renderEmptyMessages("");
        }

        const requestId = ++state.messageRequestId;

        try {
            const messages = await listMessages(conversation.conversationId);
            if (requestId !== state.messageRequestId) {
                return;
            }

            const signature = messageSignature(messages);
            const previousSignature = state.messageSignatures[conversation.conversationId] || "";
            const shouldRender = settings.forceRender
                || state.renderedConversationId !== conversation.conversationId
                || previousSignature !== signature;

            state.messagesByConversationId[conversation.conversationId] = messages;
            state.messageSignatures[conversation.conversationId] = signature;

            if (shouldRender) {
                renderMessages(messages);
                state.renderedConversationId = conversation.conversationId;
            } else {
                scrollMessagesToLatest();
            }

            const shouldMarkRead = conversation.unreadCount > 0
                && (settings.markAsRead || previousSignature !== signature);

            if (shouldMarkRead) {
                await markRead(conversation.conversationId);
                conversation.unreadCount = 0;
                state.conversationListSignature = conversationSignature(state.conversations);
                renderConversations();
            }

            setStatus("", false);
        } catch (error) {
            if (!state.messagesByConversationId[conversation.conversationId]) {
                renderEmptyMessages(error.message || "Failed to load messages.");
            }
            setStatus(error.message || "Failed to load messages.", true);
        }
    }

    async function handleSend() {
        if (!state.activeConversationId || elements.send.disabled) {
            return;
        }

        const body = elements.input.value.trim();
        const attachment = state.selectedAttachment;
        if (!body && !attachment) {
            return;
        }

        const attachmentError = validateComposerAttachment(attachment);
        if (attachmentError) {
            setStatus(attachmentError, true);
            return;
        }

        elements.send.disabled = true;
        elements.attachmentButton.disabled = true;
        elements.attachmentInput.disabled = true;

        try {
            await sendMessage(state.activeConversationId, body, attachment);
            elements.input.value = "";
            clearComposerAttachment();
            await refreshConversations({ silent: true, forceMessageReload: true });
        } catch (error) {
            setStatus(error.message || "Failed to send message.", true);
        } finally {
            const activeConversation = getActiveConversation();
            const enabled = Boolean(activeConversation) && activeConversation.canReply !== false;
            setComposerEnabled(enabled);
        }
    }

    function startPolling() {
        if (state.pollHandle) {
            window.clearInterval(state.pollHandle);
        }

        state.pollHandle = window.setInterval(function () {
            if (document.hidden) {
                return;
            }

            refreshConversations({ silent: true, preserveStatus: true });
        }, POLL_INTERVAL_MS);
    }

    elements.search.addEventListener("input", function (event) {
        state.search = event.target.value || "";
        renderConversations();
    });

    elements.attachmentButton.addEventListener("click", function () {
        if (elements.attachmentButton.disabled) {
            return;
        }

        elements.attachmentInput.click();
    });
    elements.attachmentInput.addEventListener("change", function (event) {
        const input = event.target;
        const file = input.files && input.files.length ? input.files[0] : null;

        if (!file) {
            clearComposerAttachment();
            return;
        }

        const attachmentError = validateComposerAttachment(file);
        if (attachmentError) {
            clearComposerAttachment();
            setStatus(attachmentError, true);
            return;
        }

        clearComposerAttachment();
        state.selectedAttachment = file;
        state.selectedAttachmentPreviewUrl = URL.createObjectURL(file);
        renderComposerAttachmentPreview();
        setStatus("", false);
    });
    elements.send.addEventListener("click", handleSend);
    elements.input.addEventListener("keydown", function (event) {
        if (event.key !== "Enter" || event.shiftKey) {
            return;
        }

        event.preventDefault();
        handleSend();
    });
    elements.refresh.addEventListener("click", function () {
        refreshConversations({ forceMessageReload: true });
    });

    document.addEventListener("visibilitychange", function () {
        if (!document.hidden) {
            refreshConversations({ silent: true, preserveStatus: true });
        }
    });

    if (config.apiMode === "dev" && !config.devMessagingEnabled) {
        setStatus("Dev messaging is disabled in the current environment. Switch to main mode or enable the feature flag.", true);
        setComposerEnabled(false);
        renderEmptyMessages("");
        return;
    }

    setComposerEnabled(false);
    refreshConversations();
    startPolling();
})();
