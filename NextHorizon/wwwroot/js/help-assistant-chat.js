document.addEventListener("DOMContentLoaded", () => {
    const page = document.querySelector("[data-help-view='assistant']");
    if (!page) {
        return;
    }

    const config = {
        botGreeting: "Hi. Select a topic above or ask a question.",
        botNoMatch: "No FAQ matches found. Choose a category, quick action, or refine your question.",
        agentGreeting: "An agent will assist you shortly.",
        agentSelectCategoryMessage: "Select a category first to continue with Live Agent.",
        agentWaitingStatus: "Waiting for an available agent",
        agentWaitingReply: "An agent will assist you shortly.",
        agentQueuedHint: "An agent will assist you shortly. Your messages will stay in this queue.",
        agentCategoryLockedHint: "Your Live Agent conversation is locked to the selected category until it is resolved or cleared.",
        quickActionsError: "Quick actions are unavailable right now.",
        sessionCreateError: "Unable to start the Live Agent conversation right now.",
        sessionQuestionError: "Unable to save your Live Agent question right now.",
        sessionResolveError: "Unable to resolve the current Live Agent conversation right now.",
        botTypingDelay: 500,
        agentTypingDelay: 1000,
        maxSearchSuggestions: 3,
        maxCategorySuggestions: 5,
        quickActionLimit: 6,
        defaultInputPlaceholder: "Type your message here...",
        categoryRequiredPlaceholder: "Select a category first...",
        agentInputPlaceholder: "Describe your concern for the selected category...",
    };

    const state = {
        currentMode: "bot",
        conversations: {
            bot: [],
            agent: [],
        },
        agentAvailability: "queued",
        agentTransaction: {
            selectedCategorySlug: "",
            selectedCategoryTitle: "",
            sessionId: null,
            supportFaqId: null,
            firstQuestionCaptured: false,
            isStartingSession: false,
        },
        csrfTokenPromise: null,
        isTyping: false,
    };

    const elements = {
        chatBox: page.querySelector("#helpChatBox"),
        chatInput: page.querySelector("#chatInput"),
        sendBtn: page.querySelector("#sendBtn"),
        clearBtn: page.querySelector("#clearChatBtn"),
        resolveBtn: page.querySelector("#resolveChatBtn"),
        modeBtns: page.querySelectorAll(".mode-btn"),
        modeIcon: page.querySelector("#chatModeIcon"),
        modeLabel: page.querySelector("#chatModeLabel"),
        statusDot: page.querySelector(".status-dot"),
        statusText: page.querySelector(".status-text"),
        chatHint: page.querySelector("#chatHint"),
        quickSolutions: page.querySelector("#quickSolutions"),
        quickToggle: page.querySelector("#toggleQuickSolutions"),
        quickActions: page.querySelector("[data-help-quick-actions]"),
    };

    const escapeHtml = (value) => {
        if (typeof value !== "string") {
            return "";
        }

        return value
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#39;");
    };

    const fetchJson = async (url, options) => {
        const response = await fetch(url, options);
        let payload = null;

        if (response.status !== 204) {
            const contentType = response.headers.get("content-type") || "";
            payload = contentType.includes("application/json")
                ? await response.json()
                : await response.text();
        }

        if (!response.ok) {
            const message = payload && typeof payload === "object" && payload.message
                ? payload.message
                : response.status === 401
                    ? "You need to sign in before starting a Live Agent conversation."
                    : "Unable to load help content right now.";
            throw new Error(message);
        }

        return payload;
    };

    const ensureCsrfToken = async () => {
        if (!state.csrfTokenPromise) {
            state.csrfTokenPromise = fetchJson("/api/security/csrf-token").then((result) => result.token);
        }

        return state.csrfTokenPromise;
    };

    const scrollToBottom = () => {
        if (elements.chatBox) {
            elements.chatBox.scrollTop = elements.chatBox.scrollHeight;
        }
    };

    const hasSelectedAgentCategory = () => !!state.agentTransaction.selectedCategorySlug;

    const emitCategoryLockChange = () => {
        document.dispatchEvent(new CustomEvent("help-assistant:category-lock-changed", {
            detail: {
                locked: state.currentMode === "agent" && hasSelectedAgentCategory(),
                selectedSlug: state.agentTransaction.selectedCategorySlug,
            },
        }));
    };

    const syncQuickActions = () => {
        elements.quickActions?.querySelectorAll(".quick-solution-btn").forEach((button) => {
            const slug = button.dataset.categorySlug || "";
            const isSelected = state.currentMode === "agent"
                && hasSelectedAgentCategory()
                && slug === state.agentTransaction.selectedCategorySlug;
            const isDisabled = state.currentMode === "agent"
                && hasSelectedAgentCategory()
                && slug !== state.agentTransaction.selectedCategorySlug;

            button.classList.toggle("is-selected", isSelected);
            button.disabled = isDisabled;
            button.setAttribute("aria-disabled", isDisabled.toString());
        });
    };

    const syncAgentComposerState = () => {
        if (!elements.chatInput || !elements.sendBtn || !elements.resolveBtn) {
            return;
        }

        const isAgent = state.currentMode === "agent";
        const hasCategory = hasSelectedAgentCategory();

        if (!isAgent) {
            elements.chatInput.disabled = false;
            elements.chatInput.placeholder = config.defaultInputPlaceholder;
            elements.sendBtn.disabled = false;
            elements.resolveBtn.hidden = true;
            elements.resolveBtn.disabled = true;
            return;
        }

        elements.resolveBtn.hidden = false;
        elements.resolveBtn.disabled = !state.agentTransaction.sessionId;
        elements.chatInput.disabled = !hasCategory;
        elements.chatInput.placeholder = hasCategory
            ? config.agentInputPlaceholder
            : config.categoryRequiredPlaceholder;
        elements.sendBtn.disabled = !hasCategory;
    };

    const resetAgentTransaction = () => {
        state.agentTransaction = {
            selectedCategorySlug: "",
            selectedCategoryTitle: "",
            sessionId: null,
            supportFaqId: null,
            firstQuestionCaptured: false,
            isStartingSession: false,
        };
        syncQuickActions();
        syncAgentComposerState();
        emitCategoryLockChange();
    };

    const createSuggestionMarkup = (label, items, emptyMessage) => {
        if (!Array.isArray(items) || !items.length) {
            return `<div class="chat-no-match">${escapeHtml(emptyMessage)}</div>`;
        }

        const buttons = items
            .map((item) => {
                const question = item.question || "";
                const answer = item.answer || "";
                const categoryTitle = item.categoryTitle || "";
                return `
                    <button
                        type="button"
                        class="chat-result-btn"
                        data-chat-question="${escapeHtml(question)}"
                        data-chat-answer="${escapeHtml(answer)}">
                        <span class="chat-result-question">${escapeHtml(question)}</span>
                        ${categoryTitle ? `<span class="chat-result-meta">${escapeHtml(categoryTitle)}</span>` : ""}
                    </button>`;
            })
            .join("");

        return `
            <div class="chat-results">
                <div class="chat-results-label">${escapeHtml(label)}</div>
                <div class="chat-results-actions">${buttons}</div>
            </div>`;
    };

    const getGreeting = (mode) => {
        if (mode === "agent") {
            return hasSelectedAgentCategory()
                ? config.agentGreeting
                : config.agentSelectCategoryMessage;
        }

        return config.botGreeting;
    };

    const renderConversation = (mode) => {
        if (!elements.chatBox) {
            return;
        }

        const messages = state.conversations[mode];
        if (!messages.length) {
            const greeting = getGreeting(mode);
            elements.chatBox.innerHTML = `<div class="chat-message ${mode}">${escapeHtml(greeting)}</div>`;
            scrollToBottom();
            return;
        }

        elements.chatBox.innerHTML = messages
            .map((message) => {
                if (message.html) {
                    return `<div class="chat-message ${message.type}">${message.html}</div>`;
                }

                return `<div class="chat-message ${message.type}">${escapeHtml(message.text || "")}</div>`;
            })
            .join("");
        scrollToBottom();
    };

    const addTextMessage = (mode, type, text) => {
        state.conversations[mode].push({ type, text });
        if (mode !== state.currentMode || !elements.chatBox) {
            return;
        }

        const node = document.createElement("div");
        node.className = `chat-message ${type}`;
        node.textContent = text;
        elements.chatBox.appendChild(node);
        scrollToBottom();
    };

    const addHtmlMessage = (mode, type, html) => {
        state.conversations[mode].push({ type, html });
        if (mode !== state.currentMode || !elements.chatBox) {
            return;
        }

        const node = document.createElement("div");
        node.className = `chat-message ${type}`;
        node.innerHTML = html;
        elements.chatBox.appendChild(node);
        scrollToBottom();
    };

    const showTypingIndicator = (mode = state.currentMode) => {
        if (state.isTyping || !elements.chatBox) {
            return null;
        }

        state.isTyping = true;
        const indicator = document.createElement("div");
        indicator.className = `chat-message ${mode === "agent" ? "agent" : "bot"} loading`;
        indicator.innerHTML = "<div class=\"typing\"><span></span><span></span><span></span></div>";
        elements.chatBox.appendChild(indicator);
        scrollToBottom();
        return indicator;
    };

    const hideTypingIndicator = (indicator) => {
        if (indicator && indicator.parentNode) {
            indicator.remove();
        }

        state.isTyping = false;
    };

    const updateModeUi = (mode) => {
        elements.modeBtns.forEach((button) => {
            button.classList.toggle("active", button.dataset.mode === mode);
        });

        if (mode === "agent") {
            elements.modeIcon.className = "fas fa-headset";
            elements.modeLabel.textContent = "Live Agent";
            elements.statusDot.classList.remove("online");
            elements.statusDot.classList.add("waiting");
            elements.statusText.textContent = config.agentWaitingStatus;
            elements.statusText.classList.add("agent-waiting");
            elements.chatHint.textContent = hasSelectedAgentCategory()
                ? config.agentCategoryLockedHint
                : config.agentSelectCategoryMessage;
            syncAgentComposerState();
            syncQuickActions();
            emitCategoryLockChange();
            return;
        }

        elements.modeIcon.className = "fas fa-robot";
        elements.modeLabel.textContent = "Quick Help Chat";
        elements.statusDot.classList.add("online");
        elements.statusDot.classList.remove("waiting");
        elements.statusText.textContent = "Bot ready to help";
        elements.statusText.classList.remove("agent-waiting");
        elements.chatHint.textContent = "Tip: Click a category or question for instant help.";
        syncAgentComposerState();
        syncQuickActions();
        emitCategoryLockChange();
    };

    const switchMode = (mode) => {
        if (state.currentMode === mode) {
            return;
        }

        state.currentMode = mode;
        updateModeUi(mode);
        renderConversation(mode);
        elements.chatInput?.focus();
    };

    const activateCategory = (slug) => {
        if (!slug) {
            return;
        }

        document.dispatchEvent(new CustomEvent("help-assistant:activate-category", {
            detail: { slug },
        }));
    };

    const renderQuickActions = (categories) => {
        if (!elements.quickActions) {
            return;
        }

        const items = Array.isArray(categories) ? categories.slice(0, config.quickActionLimit) : [];
        if (!items.length) {
            elements.quickActions.innerHTML = `<div class="help-empty-state">${escapeHtml(config.quickActionsError)}</div>`;
            return;
        }

        elements.quickActions.innerHTML = "";
        items.forEach((category, index) => {
            const button = document.createElement("button");
            button.type = "button";
            button.className = `quick-solution-btn${index === 0 ? " primary" : ""}`;
            button.dataset.categorySlug = category.slug || "";
            button.innerHTML = `<span>${escapeHtml(category.title || "")}</span>`;
            elements.quickActions.appendChild(button);
        });

        syncQuickActions();
    };

    const loadQuickActions = async () => {
        if (!elements.quickActions) {
            return;
        }

        try {
            const categories = await fetchJson("/api/help/categories");
            renderQuickActions(categories);
        } catch (error) {
            elements.quickActions.innerHTML = `<div class="help-error-state">${escapeHtml(error.message)}</div>`;
        }
    };

    const createLiveAgentSession = async (categorySlug) => {
        const token = await ensureCsrfToken();
        const payload = {
            categorySlug,
        };

        return fetchJson("/api/help/live-agent/sessions", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": token,
            },
            body: JSON.stringify(payload),
        });
    };

    const captureLiveAgentQuestion = async (sessionId, message) => {
        const token = await ensureCsrfToken();
        return fetchJson(`/api/help/live-agent/sessions/${encodeURIComponent(sessionId)}/question`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": token,
            },
            body: JSON.stringify({ message }),
        });
    };

    const resolveLiveAgentSession = async (sessionId) => {
        const token = await ensureCsrfToken();
        return fetchJson(`/api/help/live-agent/sessions/${encodeURIComponent(sessionId)}/resolve`, {
            method: "POST",
            headers: {
                "X-CSRF-TOKEN": token,
            },
        });
    };

    const selectFaq = (question, answer, mode = state.currentMode) => {
        if (!question || !answer) {
            return;
        }

        if (mode !== state.currentMode) {
            switchMode(mode);
        }

        addTextMessage(mode, "user", question);
        addTextMessage(mode, mode === "agent" ? "agent" : "bot", answer);
    };

    const handleBotSearch = async (text) => {
        const indicator = showTypingIndicator("bot");

        try {
            const results = await fetchJson(`/api/help/search?query=${encodeURIComponent(text)}`);
            window.setTimeout(() => {
                hideTypingIndicator(indicator);

                const matches = Array.isArray(results)
                    ? results
                        .slice(0, config.maxSearchSuggestions)
                        .map((result) => ({
                            question: result.question,
                            answer: result.answer,
                            categoryTitle: result.categoryTitle,
                        }))
                    : [];

                const html = createSuggestionMarkup(
                    "Select the FAQ that matches your question.",
                    matches,
                    config.botNoMatch);

                addHtmlMessage("bot", "bot", html);
            }, config.botTypingDelay);
        } catch (error) {
            window.setTimeout(() => {
                hideTypingIndicator(indicator);
                addTextMessage("bot", "bot", error.message);
            }, config.botTypingDelay);
        }
    };

    const handleAgentResponse = () => {
        const indicator = showTypingIndicator("agent");
        window.setTimeout(() => {
            hideTypingIndicator(indicator);
            addTextMessage("agent", "agent", config.agentWaitingReply);
        }, config.agentTypingDelay);
    };

    const setSelectedAgentCategory = async (detail) => {
        const slug = detail?.slug || "";
        const title = detail?.title || "";
        if (!slug || !title) {
            return false;
        }

        if (hasSelectedAgentCategory()) {
            return state.agentTransaction.selectedCategorySlug === slug;
        }

        if (state.agentTransaction.isStartingSession) {
            return false;
        }

        state.agentTransaction.isStartingSession = true;

        try {
            const session = await createLiveAgentSession(slug);
            state.agentTransaction.selectedCategorySlug = session.categorySlug || slug;
            state.agentTransaction.selectedCategoryTitle = session.categoryTitle || title;
            state.agentTransaction.sessionId = session.sessionId;
            state.agentTransaction.supportFaqId = session.supportFaqId;
            state.agentTransaction.firstQuestionCaptured = false;
            return true;
        } catch (error) {
            addTextMessage("agent", "agent", error.message || config.sessionCreateError);
            return false;
        } finally {
            state.agentTransaction.isStartingSession = false;
            syncQuickActions();
            syncAgentComposerState();
            emitCategoryLockChange();
        }
    };

    const handleCategorySelection = async (detail, mode = state.currentMode) => {
        const title = detail?.title || "";
        const slug = detail?.slug || "";
        const faqs = Array.isArray(detail?.faqs)
            ? detail.faqs
                .slice(0, config.maxCategorySuggestions)
                .map((faq) => ({
                    question: faq.question,
                    answer: faq.answer,
                    categoryTitle: title,
                }))
            : [];

        if (!title) {
            return;
        }

        if (mode !== state.currentMode) {
            switchMode(mode);
        }

        if (mode === "agent") {
            const accepted = await setSelectedAgentCategory({ slug, title });
            if (!accepted) {
                return;
            }
        }

        addTextMessage(mode, "user", title);
        addHtmlMessage(
            mode,
            mode === "agent" ? "agent" : "bot",
            createSuggestionMarkup(
                `Select a ${title} FAQ.`,
                faqs,
                config.botNoMatch)
        );
    };

    const sendMessage = async () => {
        const text = elements.chatInput?.value.trim();
        if (!text) {
            return;
        }

        const mode = state.currentMode;
        if (mode === "agent" && !hasSelectedAgentCategory()) {
            addTextMessage("agent", "agent", config.agentSelectCategoryMessage);
            syncAgentComposerState();
            return;
        }

        addTextMessage(mode, "user", text);
        elements.chatInput.value = "";

        if (mode === "agent") {
            if (!state.agentTransaction.sessionId) {
                addTextMessage("agent", "agent", config.sessionCreateError);
                return;
            }

            if (!state.agentTransaction.firstQuestionCaptured) {
                try {
                    await captureLiveAgentQuestion(state.agentTransaction.sessionId, text);
                    state.agentTransaction.firstQuestionCaptured = true;
                    syncAgentComposerState();
                } catch (error) {
                    addTextMessage("agent", "agent", error.message || config.sessionQuestionError);
                    return;
                }
            }

            handleAgentResponse();
            return;
        }

        handleBotSearch(text);
    };

    const clearConversation = async () => {
        if (state.currentMode === "agent" && state.agentTransaction.sessionId) {
            try {
                await resolveLiveAgentSession(state.agentTransaction.sessionId);
            } catch (error) {
                addTextMessage("agent", "agent", error.message || config.sessionResolveError);
                return;
            }

            resetAgentTransaction();
        }

        if (state.currentMode === "agent" && !state.agentTransaction.sessionId) {
            resetAgentTransaction();
        }

        state.conversations[state.currentMode] = [];
        updateModeUi(state.currentMode);
        renderConversation(state.currentMode);
    };

    const resolveConversation = async () => {
        if (!state.agentTransaction.sessionId) {
            return;
        }

        try {
            await resolveLiveAgentSession(state.agentTransaction.sessionId);
        } catch (error) {
            addTextMessage("agent", "agent", error.message || config.sessionResolveError);
            return;
        }

        resetAgentTransaction();
        state.conversations.agent = [];
        updateModeUi("agent");
        renderConversation("agent");
    };

    const bindEvents = () => {
        elements.modeBtns.forEach((button) => {
            button.addEventListener("click", () => switchMode(button.dataset.mode || "bot"));
        });

        elements.clearBtn?.addEventListener("click", async () => {
            await clearConversation();
        });

        elements.resolveBtn?.addEventListener("click", async () => {
            await resolveConversation();
        });

        elements.sendBtn?.addEventListener("click", async () => {
            await sendMessage();
        });

        elements.chatInput?.addEventListener("keydown", async (event) => {
            if (event.key === "Enter") {
                event.preventDefault();
                await sendMessage();
            }
        });

        elements.chatInput?.addEventListener("input", () => {
            syncAgentComposerState();
        });

        elements.quickActions?.addEventListener("click", (event) => {
            const button = event.target.closest(".quick-solution-btn");
            if (!button || button.disabled) {
                return;
            }

            activateCategory(button.dataset.categorySlug || "");
        });

        elements.quickToggle?.addEventListener("click", () => {
            if (!elements.quickSolutions) {
                return;
            }

            elements.quickSolutions.classList.toggle("collapsed");
            const collapsed = elements.quickSolutions.classList.contains("collapsed");
            elements.quickToggle.innerHTML = collapsed
                ? "<span>Show</span><i class=\"fas fa-chevron-down\"></i>"
                : "<span>Hide</span><i class=\"fas fa-chevron-up\"></i>";
        });

        elements.chatBox?.addEventListener("click", (event) => {
            const button = event.target.closest(".chat-result-btn");
            if (!button) {
                return;
            }

            selectFaq(button.dataset.chatQuestion || "", button.dataset.chatAnswer || "", state.currentMode);
        });

        document.addEventListener("help-assistant:faq-selected", (event) => {
            const detail = event.detail || {};
            selectFaq(detail.question || "", detail.answer || "", state.currentMode);
        });

        document.addEventListener("help-assistant:category-selected", async (event) => {
            await handleCategorySelection(event.detail || {}, state.currentMode);
        });
    };

    bindEvents();
    updateModeUi("bot");
    renderConversation("bot");
    loadQuickActions();
});
