(function () {
    const config = window.sellerMessengerConfig || window.messengerPageConfig;
    const root = document.querySelector("[data-seller-messenger-root]") || document.querySelector("[data-messenger-root]");

    if (!config || !root) {
        return;
    }

    const role = "seller";
    const pollIntervalMs = 5000;
    let csrfTokenPromise = null;

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
        attachmentPreview: root.querySelector("[data-composer-attachment-preview]"),
        send: root.querySelector("[data-send-message]"),
        refresh: root.querySelector("[data-refresh-conversations]")
    };

    const state = {
        conversations: [],
        activeConversationId: null,
        currentUserId: String(config.actorUserId || config.currentUserId || ""),
        search: "",
        selectedAttachment: null,
        selectedAttachmentPreviewUrl: null,
        isRefreshingConversations: false,
        pollHandle: null
    };

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

    function parseResponse(response) {
        return response.text().then(function (text) {
            if (!text) {
                return null;
            }

            try {
                return JSON.parse(text);
            } catch (_) {
                return text;
            }
        });
    }

    function normalizeConversation(item) {
        return {
            conversationId: Number(item.conversationId || item.ConversationId || 0),
            currentUserId: String(item.currentUserId || item.CurrentUserId || ""),
            displayName: String(item.displayName || item.DisplayName || ""),
            displaySubtitle: String(item.displaySubtitle || item.DisplaySubtitle || ""),
            avatarUrl: String(item.avatarUrl || item.AvatarUrl || ""),
            contextLabel: String(item.contextLabel || item.ContextLabel || ""),
            counterpartyId: String(item.counterpartyId || item.CounterpartyId || ""),
            canReply: item.canReply !== undefined ? Boolean(item.canReply) : Boolean(item.CanReply),
            orderId: item.orderId || item.OrderId || null,
            lastMessagePreview: String(item.lastMessagePreview || item.LastMessagePreview || ""),
            unreadCount: Number(item.unreadCount || item.UnreadCount || 0),
            updatedAt: item.updatedAt || item.UpdatedAt || item.lastMessageAt || item.LastMessageAt || null
        };
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

    function setStatus(message, isError) {
        if (!elements.status) {
            return;
        }

        const text = String(message || "").trim();
        elements.status.textContent = text;
        elements.status.hidden = !text;
        elements.status.classList.toggle("is-error", Boolean(text) && Boolean(isError));
    }

    function setComposerEnabled(enabled) {
        const isEnabled = Boolean(enabled);
        elements.input.disabled = !isEnabled;
        elements.attachmentButton.disabled = !isEnabled;
        elements.attachmentInput.disabled = !isEnabled;
        elements.send.disabled = !isEnabled;
    }

    function initialsFromName(name) {
        const parts = String(name || "")
            .split(/\s+/)
            .map(function (value) { return value.trim(); })
            .filter(Boolean);

        if (parts.length === 0) {
            return "--";
        }

        return parts.slice(0, 2).map(function (part) {
            return part.charAt(0).toUpperCase();
        }).join("");
    }

    function formatTime(value) {
        const date = parseUtcDate(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleTimeString([], { hour: "numeric", minute: "2-digit" });
    }

    function formatDateTime(value) {
        const date = parseUtcDate(value);
        if (Number.isNaN(date.getTime())) {
            return "";
        }

        return date.toLocaleString([], { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" });
    }

    function parseUtcDate(value) {
        if (!value) {
            return new Date("");
        }

        if (value instanceof Date) {
            return value;
        }

        const source = String(value).trim();
        if (!source) {
            return new Date("");
        }

        const hasTimeZone = /([zZ]|[+\-]\d{2}:\d{2})$/.test(source);
        return new Date(hasTimeZone ? source : source + "Z");
    }

    function attachmentExtension(value) {
        const source = String(value || "").split("#")[0].split("?")[0];
        const lastSegment = source.split("/").pop() || "";
        const lastDot = lastSegment.lastIndexOf(".");
        return lastDot >= 0 ? lastSegment.slice(lastDot + 1).toLowerCase() : "";
    }

    function attachmentKind(value) {
        const source = String(value || "").toLowerCase();

        // Binary-backed chat attachments are served from the API without a file extension.
        if (source.includes("/api/messages/messages/") && source.includes("/attachment")) {
            return "image";
        }

        const extension = attachmentExtension(value);
        if (["jpg", "jpeg", "png", "webp"].includes(extension)) {
            return "image";
        }

        if (["mp4", "webm", "mov"].includes(extension)) {
            return "video";
        }

        return "file";
    }

    function canUseAttachment(file) {
        if (!file) {
            return "";
        }

        const allowedTypes = ["image/jpeg", "image/png", "image/webp"];
        const allowedExtensions = ["jpg", "jpeg", "png", "webp"];
        const extension = attachmentExtension(file.name || "");
        const type = String(file.type || "").toLowerCase();

        if (Number(file.size || 0) > 5 * 1024 * 1024) {
            return "Attachment must be 5MB or smaller.";
        }

        if (!allowedTypes.includes(type) && !allowedExtensions.includes(extension)) {
            return "Attachment must be a jpg, jpeg, png, or webp image.";
        }

        return "";
    }

    function formatBytes(bytes) {
        const value = Number(bytes || 0);
        if (value <= 0) {
            return "";
        }

        if (value < 1024 * 1024) {
            return Math.max(1, Math.round(value / 1024)) + " KB";
        }

        return (value / (1024 * 1024)).toFixed(1) + " MB";
    }

    function activeConversation() {
        return state.conversations.find(function (item) {
            return item.conversationId === state.activeConversationId;
        }) || null;
    }

    function syncConversationHeader(conversation) {
        if (!conversation) {
            elements.title.textContent = "Conversation";
            elements.subtitle.textContent = "Select a conversation to start messaging.";
            elements.avatar.textContent = "--";
            setComposerEnabled(false);
            return;
        }

        elements.title.textContent = conversation.displayName || "Conversation";
        elements.subtitle.textContent = conversation.displaySubtitle || conversation.contextLabel || "General inquiry";
        elements.avatar.textContent = initialsFromName(conversation.displayName);

        if (conversation.currentUserId) {
            state.currentUserId = conversation.currentUserId;
        }

        setComposerEnabled(conversation.canReply !== false);
    }

    function filteredConversations() {
        const query = state.search.trim().toLowerCase();
        if (!query) {
            return state.conversations;
        }

        return state.conversations.filter(function (conversation) {
            return [
                conversation.displayName,
                conversation.displaySubtitle,
                conversation.contextLabel,
                conversation.lastMessagePreview,
                conversation.orderId ? "order " + conversation.orderId : ""
            ].join(" ").toLowerCase().includes(query);
        });
    }

    function renderConversationList() {
        const conversations = filteredConversations();
        elements.list.innerHTML = "";
        elements.noResults.hidden = conversations.length > 0;

        conversations.forEach(function (conversation) {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "conversation-item" + (conversation.conversationId === state.activeConversationId ? " active" : "");

            const avatar = document.createElement("div");
            avatar.className = "conversation-avatar";
            avatar.innerHTML = '<div class="avatar-placeholder">' + initialsFromName(conversation.displayName) + "</div>";

            const info = document.createElement("div");
            info.className = "conversation-info";

            const name = document.createElement("div");
            name.className = "conversation-name";
            name.textContent = conversation.displayName || "Conversation";

            const meta = document.createElement("div");
            meta.className = "conversation-meta";

            const preview = document.createElement("span");
            preview.className = "last-message";
            preview.textContent = conversation.lastMessagePreview || "No messages yet.";
            meta.appendChild(preview);

            if (conversation.unreadCount > 0) {
                const unread = document.createElement("span");
                unread.className = "unread-badge";
                unread.textContent = String(conversation.unreadCount);
                meta.appendChild(unread);
            }

            const time = document.createElement("div");
            time.className = "conversation-time";
            time.textContent = formatDateTime(conversation.updatedAt);

            info.appendChild(name);
            info.appendChild(meta);
            info.appendChild(time);
            item.appendChild(avatar);
            item.appendChild(info);

            item.addEventListener("click", function () {
                if (conversation.conversationId === state.activeConversationId) {
                    return;
                }

                state.activeConversationId = conversation.conversationId;
                renderConversationList();
                syncConversationHeader(conversation);
                loadMessages(true);
            });

            elements.list.appendChild(item);
        });
    }

    function renderEmptyMessages(message) {
        elements.messages.innerHTML = "";
        const empty = document.createElement("div");
        empty.className = "empty-state";
        empty.textContent = message || "";
        elements.messages.appendChild(empty);
    }

    function scrollMessagesToBottom() {
        window.requestAnimationFrame(function () {
            elements.messages.scrollTop = elements.messages.scrollHeight;
        });
    }

    function ensureImageViewer() {
        if (state.imageViewer) {
            return state.imageViewer;
        }

        const viewer = document.createElement("div");
        viewer.className = "messenger-image-viewer";
        viewer.hidden = true;
        viewer.innerHTML = [
            '<button type="button" class="messenger-image-viewer__backdrop" data-image-viewer-close aria-label="Close image viewer"></button>',
            '<div class="messenger-image-viewer__dialog" role="dialog" aria-modal="true" aria-label="Image viewer">',
            '  <button type="button" class="messenger-image-viewer__close" data-image-viewer-close aria-label="Close image viewer">&times;</button>',
            '  <img class="messenger-image-viewer__image" alt="Message attachment preview">',
            '</div>'
        ].join("");

        viewer.querySelectorAll("[data-image-viewer-close]").forEach(function (button) {
            button.addEventListener("click", closeImageViewer);
        });

        const image = viewer.querySelector(".messenger-image-viewer__image");
        const dialog = viewer.querySelector(".messenger-image-viewer__dialog");
        dialog.addEventListener("click", function (event) {
            event.stopPropagation();
        });

        document.body.appendChild(viewer);
        state.imageViewer = viewer;
        state.imageViewerFrame = dialog;
        state.imageViewerImage = image;
        return viewer;
    }

    function openImageViewer(url) {
        const viewer = ensureImageViewer();
        state.imageViewerImage.src = url;
        viewer.hidden = false;
        document.body.classList.add("messenger-image-viewer-open");
    }

    function closeImageViewer() {
        if (!state.imageViewer) {
            return;
        }

        state.imageViewer.hidden = true;
        if (state.imageViewerImage) {
            state.imageViewerImage.removeAttribute("src");
        }
        document.body.classList.remove("messenger-image-viewer-open");
    }
    function createAttachmentPreview(url) {
        const kind = attachmentKind(url);
        const wrapper = document.createElement("div");
        wrapper.className = "message-attachment";

        if (kind === "image") {
            const button = document.createElement("button");
            button.type = "button";
            button.className = "attachment-preview-button";
            button.addEventListener("click", function () {
                openImageViewer(url);
            });

            const image = document.createElement("img");
            image.className = "attachment-preview attachment-image";
            image.src = url;
            image.alt = "Message attachment";
            button.appendChild(image);
            wrapper.appendChild(button);
            return wrapper;
        }

        if (kind === "video") {
            const video = document.createElement("video");
            video.className = "attachment-preview attachment-video";
            video.src = url;
            video.controls = true;
            video.preload = "metadata";
            wrapper.appendChild(video);
            return wrapper;
        }

        const link = document.createElement("a");
        link.className = "attachment-link";
        link.href = url;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        link.textContent = "Open attachment";
        wrapper.appendChild(link);
        return wrapper;
    }

    function renderMessages(messages) {
        elements.messages.innerHTML = "";

        if (!messages.length) {
            renderEmptyMessages("No messages yet.");
            return;
        }

        messages.forEach(function (message) {
            const isSent = message.senderUserId === state.currentUserId;
            const wrapper = document.createElement("div");
            wrapper.className = "message " + (isSent ? "sent" : "received");

            const content = document.createElement("div");
            content.className = "message-content";

            if (message.isDeleted || message.body) {
                const text = document.createElement("p");
                text.textContent = message.isDeleted ? "[message deleted]" : message.body;
                content.appendChild(text);
            }

            if (message.attachmentUrl) {
                content.appendChild(createAttachmentPreview(message.attachmentUrl));
            }

            const time = document.createElement("span");
            time.className = "message-time";
            time.textContent = formatTime(message.sentAt);

            wrapper.appendChild(content);
            wrapper.appendChild(time);
            elements.messages.appendChild(wrapper);
        });

        scrollMessagesToBottom();
    }

    function buildHeaders(includeJson) {
        const headers = {};
        if (includeJson) {
            headers["Content-Type"] = "application/json";
        }
        return headers;
    }

    function request(path, options, query) {
        return fetch(buildUrl(path, query), Object.assign({
            credentials: "same-origin"
        }, options || {})).then(function (response) {
            return parseResponse(response).then(function (payload) {
                if (response.ok) {
                    return payload;
                }

                const message = typeof payload === "string"
                    ? payload
                    : payload && payload.title
                        ? payload.title
                        : "Messaging request failed.";
                throw new Error(message);
            });
        });
    }

    function getCsrfToken() {
        if (!csrfTokenPromise) {
            csrfTokenPromise = request("/api/security/csrf-token", { headers: buildHeaders(false) })
                .then(function (payload) {
                    return payload && payload.token ? payload.token : "";
                })
                .catch(function (error) {
                    csrfTokenPromise = null;
                    throw error;
                });
        }

        return csrfTokenPromise;
    }

    function requestWithCsrf(path, options, query) {
        return getCsrfToken().then(function (token) {
            const headers = Object.assign({}, options && options.headers ? options.headers : {});
            if (token) {
                headers["X-CSRF-TOKEN"] = token;
            }

            return request(path, Object.assign({}, options || {}, { headers: headers }), query);
        });
    }

    function chooseInitialConversationId() {
        if (!state.conversations.length) {
            return null;
        }

        const byConversationId = Number(config.initialConversationId || 0);
        if (byConversationId > 0 && state.conversations.some(function (item) { return item.conversationId === byConversationId; })) {
            return byConversationId;
        }

        const byOrderId = Number(config.orderId || 0);
        if (byOrderId > 0) {
            const matchByOrder = state.conversations.find(function (item) {
                return Number(item.orderId || 0) === byOrderId;
            });
            if (matchByOrder) {
                return matchByOrder.conversationId;
            }
        }

        const byConsumerId = String(config.consumerId || "").trim();
        if (byConsumerId) {
            const matchByConsumer = state.conversations.find(function (item) {
                return item.counterpartyId === byConsumerId;
            });
            if (matchByConsumer) {
                return matchByConsumer.conversationId;
            }
        }

        return state.conversations[0].conversationId;
    }

    function listConversations() {
        return request("/api/messages/conversations", { headers: buildHeaders(false) }, {
            role: role,
            page: 1,
            pageSize: 100
        }).then(function (payload) {
            const items = payload && Array.isArray(payload.items) ? payload.items : [];
            return items.map(normalizeConversation);
        });
    }

    function listMessages(conversationId) {
        return request("/api/messages/conversations/" + conversationId + "/messages", { headers: buildHeaders(false) }, {
            role: role,
            pageSize: 100
        }).then(function (payload) {
            return Array.isArray(payload) ? payload.map(normalizeMessage) : [];
        });
    }

    function markConversationRead(conversationId) {
        return requestWithCsrf("/api/messages/conversations/" + conversationId + "/read", {
            method: "POST",
            headers: buildHeaders(true),
            body: "{}"
        }, { role: role });
    }

    function refreshConversations(forceMessages) {
        if (state.isRefreshingConversations) {
            return Promise.resolve();
        }

        state.isRefreshingConversations = true;

        return listConversations()
            .then(function (conversations) {
                state.conversations = conversations;

                if (!conversations.length) {
                    state.activeConversationId = null;
                    renderConversationList();
                    syncConversationHeader(null);
                    renderEmptyMessages("No conversations yet.");
                    setStatus("", false);
                    return;
                }

                if (!state.activeConversationId || !conversations.some(function (item) { return item.conversationId === state.activeConversationId; })) {
                    state.activeConversationId = chooseInitialConversationId();
                }

                renderConversationList();
                syncConversationHeader(activeConversation());

                if (forceMessages !== false) {
                    return loadMessages(true);
                }

                setStatus("", false);
                return null;
            })
            .catch(function (error) {
                setStatus(error.message || "Failed to load conversations.", true);
                if (!state.conversations.length) {
                    syncConversationHeader(null);
                    renderEmptyMessages("Failed to load conversations.");
                }
            })
            .finally(function () {
                state.isRefreshingConversations = false;
            });
    }

    function loadMessages(markReadAfterLoad) {
        const conversation = activeConversation();
        if (!conversation) {
            syncConversationHeader(null);
            renderEmptyMessages("No conversation selected.");
            return Promise.resolve();
        }

        syncConversationHeader(conversation);

        return listMessages(conversation.conversationId)
            .then(function (messages) {
                renderMessages(messages);
                setStatus("", false);

                if (markReadAfterLoad && conversation.unreadCount > 0) {
                    return markConversationRead(conversation.conversationId).then(function () {
                        conversation.unreadCount = 0;
                        renderConversationList();
                    });
                }

                return null;
            })
            .catch(function (error) {
                setStatus(error.message || "Failed to load messages.", true);
                renderEmptyMessages("Failed to load messages.");
            });
    }

    function clearComposerAttachment() {
        if (state.selectedAttachmentPreviewUrl) {
            URL.revokeObjectURL(state.selectedAttachmentPreviewUrl);
        }

        state.selectedAttachment = null;
        state.selectedAttachmentPreviewUrl = null;
        elements.attachmentInput.value = "";
        elements.attachmentPreview.innerHTML = "";
        elements.attachmentPreview.hidden = true;
    }

    function renderComposerAttachmentPreview() {
        elements.attachmentPreview.innerHTML = "";

        if (!state.selectedAttachment) {
            elements.attachmentPreview.hidden = true;
            return;
        }

        const card = document.createElement("div");
        card.className = "composer-attachment-card";

        const kind = attachmentKind(state.selectedAttachment.name || "");
        if ((kind === "image" || kind === "video") && state.selectedAttachmentPreviewUrl) {
            if (kind === "image") {
                const preview = document.createElement("img");
                preview.className = "composer-attachment-media";
                preview.src = state.selectedAttachmentPreviewUrl;
                preview.alt = state.selectedAttachment.name || "Selected attachment";
                card.appendChild(preview);
            } else {
                const preview = document.createElement("video");
                preview.className = "composer-attachment-media";
                preview.src = state.selectedAttachmentPreviewUrl;
                preview.muted = true;
                preview.playsInline = true;
                preview.preload = "metadata";
                card.appendChild(preview);
            }
        } else {
            const icon = document.createElement("div");
            icon.className = "composer-attachment-file-icon";
            icon.textContent = "FILE";
            card.appendChild(icon);
        }

        const meta = document.createElement("div");
        meta.className = "composer-attachment-meta";

        const name = document.createElement("div");
        name.className = "composer-attachment-name";
        name.textContent = state.selectedAttachment.name || "Attachment";

        const details = document.createElement("div");
        details.className = "composer-attachment-details";
        details.textContent = formatBytes(state.selectedAttachment.size);

        meta.appendChild(name);
        meta.appendChild(details);
        card.appendChild(meta);

        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "composer-attachment-remove";
        remove.setAttribute("aria-label", "Remove attachment");
        remove.textContent = "×";
        remove.addEventListener("click", clearComposerAttachment);
        card.appendChild(remove);

        elements.attachmentPreview.appendChild(card);
        elements.attachmentPreview.hidden = false;
    }

    function sendMessage() {
        const conversation = activeConversation();
        if (!conversation || elements.send.disabled) {
            return Promise.resolve();
        }

        const body = elements.input.value.trim();
        const attachment = state.selectedAttachment;

        if (!body && !attachment) {
            return Promise.resolve();
        }

        const attachmentError = canUseAttachment(attachment);
        if (attachmentError) {
            setStatus(attachmentError, true);
            return Promise.resolve();
        }

        const formData = new FormData();
        formData.append("body", body);
        if (attachment) {
            formData.append("attachment", attachment);
        }

        setComposerEnabled(false);

        return requestWithCsrf("/api/messages/conversations/" + conversation.conversationId + "/messages", {
            method: "POST",
            headers: buildHeaders(false),
            body: formData
        }, { role: role })
            .then(function () {
                elements.input.value = "";
                clearComposerAttachment();
                return refreshConversations(true);
            })
            .catch(function (error) {
                setStatus(error.message || "Failed to send message.", true);
            })
            .finally(function () {
                const active = activeConversation();
                setComposerEnabled(Boolean(active) && active.canReply !== false);
            });
    }

    function startPolling() {
        if (state.pollHandle) {
            window.clearInterval(state.pollHandle);
        }

        state.pollHandle = window.setInterval(function () {
            if (document.hidden) {
                return;
            }

            refreshConversations(true);
        }, pollIntervalMs);
    }

    elements.search.addEventListener("input", function (event) {
        state.search = event.target.value || "";
        renderConversationList();
    });

    elements.attachmentButton.addEventListener("click", function () {
        if (!elements.attachmentButton.disabled) {
            elements.attachmentInput.click();
        }
    });

    elements.attachmentInput.addEventListener("change", function (event) {
        const file = event.target.files && event.target.files.length ? event.target.files[0] : null;
        if (!file) {
            clearComposerAttachment();
            return;
        }

        const attachmentError = canUseAttachment(file);
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

    elements.input.addEventListener("keydown", function (event) {
        if (event.key === "Enter" && !event.shiftKey) {
            event.preventDefault();
            sendMessage();
        }
    });

    elements.send.addEventListener("click", function () {
        sendMessage();
    });

    elements.refresh.addEventListener("click", function () {
        refreshConversations(true);
    });

    document.addEventListener("visibilitychange", function () {
        if (!document.hidden) {
            refreshConversations(true);
        }
    });

    setComposerEnabled(false);
    renderEmptyMessages("");
    refreshConversations(true);
    startPolling();
})();





