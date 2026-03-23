/**
 * Chat Component - Dual Mode (Bot/Agent) with Quick Solutions
 */

(function() {
    'use strict';

    // ================= CONFIG =================
    const CONFIG = {
        botResponses: {
            order: "You can manage your orders in 'My Purchases'. You can cancel orders before shipment or request returns after delivery.",
            tracking: "To track your order, go to 'My Purchases' and click 'Track Order' to see real-time delivery updates.",
            billing: "You can update your payment methods in Account Settings > Payment Methods.",
            account: "Manage your account by updating your profile, password, and security settings.",
            returns: "To request a return or refund, go to your order and click 'Return/Refund'.",
            technical: "Try clearing cache or switching browser. Contact support if issue continues.",
            contact: "Email us at support@nexthorizon.com or use live chat."
        },
        agentGreeting: "Hello 👋 I'm a live agent. How can I assist you today?",
        botGreeting: "Hi! Select a category above or ask me anything. 👋",
        agentTypingDelay: 1000,
        botTypingDelay: 500
    };

    // ================= STATE =================
    const state = {
        currentMode: 'bot',
        conversations: {
            bot: [],
            agent: []
        },
        isAgentTyping: false
    };

    // ================= DOM ELEMENTS =================
    let elements = {};

    function initElements() {
        elements = {
            chatContainer: document.getElementById('helpChat'),
            chatBox: document.getElementById('helpChatBox'),
            chatInput: document.getElementById('chatInput'),
            sendBtn: document.getElementById('sendBtn'),
            clearBtn: document.getElementById('clearChatBtn'),
            modeToggle: document.querySelector('.chat-mode-toggle'),
            modeBtns: document.querySelectorAll('.mode-btn'),
            modeIcon: document.getElementById('chatModeIcon'),
            modeLabel: document.getElementById('chatModeLabel'),
            statusBar: document.getElementById('chatStatusBar'),
            statusDot: document.querySelector('.status-dot'),
            statusText: document.querySelector('.status-text'),
            chatHint: document.getElementById('chatHint'),
            quickSolutions: document.getElementById('quickSolutions'),
            toggleQuickSolutions: document.getElementById('toggleQuickSolutions')
        };
    }

    // ================= MODE SWITCHING =================
    function switchMode(mode) {
        if (state.currentMode === mode) return;
        
        state.currentMode = mode;
        updateModeUI(mode);
        renderConversation(mode);
        updateChatHint(mode);
        
        if (elements.chatInput) elements.chatInput.focus();
    }

    function updateModeUI(mode) {
    elements.modeBtns.forEach(btn => {
        btn.classList.toggle('active', btn.dataset.mode === mode);
    });
    
    if (mode === 'agent') {
        elements.modeIcon.className = 'fas fa-headset';
        elements.modeLabel.textContent = 'Live Agent';
        
        // ✅ Keep status dot animated for agent mode
        elements.statusDot.classList.add('online');
        elements.statusDot.classList.add('agent-online');
        elements.statusText.textContent = 'Agent is online';
        elements.statusText.classList.add('agent-waiting');
    } else {
        elements.modeIcon.className = 'fas fa-robot';
        elements.modeLabel.textContent = 'Quick Help Chat';
        
        // ✅ Keep status dot animated for bot mode
        elements.statusDot.classList.add('online');
        elements.statusDot.classList.remove('agent-online');
        elements.statusText.textContent = 'Bot ready to help';
        elements.statusText.classList.remove('agent-waiting');
    }
}

    function updateChatHint(mode) {
        if (mode === 'agent') {
            elements.chatHint.textContent = '💬 You\'re chatting with a live agent. Be specific for faster help.';
        } else {
            elements.chatHint.textContent = '💡 Tip: Click any category card for instant help';
        }
    }

    // ================= CONVERSATION MANAGEMENT =================
    function renderConversation(mode) {
        if (!elements.chatBox) return;
        
        const messages = state.conversations[mode];
        
        if (messages.length === 0) {
            const greeting = mode === 'agent' ? CONFIG.agentGreeting : CONFIG.botGreeting;
            elements.chatBox.innerHTML = `<div class="chat-message ${mode}">${greeting}</div>`;
        } else {
            elements.chatBox.innerHTML = messages.map(msg => 
                `<div class="chat-message ${msg.type}">${msg.text}</div>`
            ).join('');
        }
        
        scrollToBottom();
    }

    function addMessage(mode, type, text) {
        state.conversations[mode].push({ type, text, timestamp: Date.now() });
        
        if (mode === state.currentMode) {
            const msgEl = document.createElement('div');
            msgEl.className = `chat-message ${type}`;
            msgEl.innerHTML = text;
            elements.chatBox.appendChild(msgEl);
            scrollToBottom();
        }
    }

    function clearConversation(mode) {
        state.conversations[mode] = [];
        
        if (mode === state.currentMode && elements.chatBox) {
            const messages = elements.chatBox.querySelectorAll('.chat-message');
            messages.forEach((msg, index) => {
                msg.style.animationDelay = `${index * 30}ms`;
                msg.classList.add('clearing');
            });
            
            setTimeout(() => {
                const greeting = mode === 'agent' ? CONFIG.agentGreeting : CONFIG.botGreeting;
                elements.chatBox.innerHTML = `<div class="chat-message ${mode}">${greeting}</div>`;
            }, 300);
        }
    }

    function scrollToBottom() {
        if (elements.chatBox) {
            elements.chatBox.scrollTop = elements.chatBox.scrollHeight;
        }
    }

    // ================= MESSAGE HELPERS =================
    function showTypingIndicator() {
        if (!elements.chatBox || state.isAgentTyping) return;
        
        state.isAgentTyping = true;
        const indicator = document.createElement('div');
        indicator.className = 'chat-message bot loading';
        indicator.id = 'typingIndicator';
        indicator.innerHTML = '<div class="typing"><span></span><span></span><span></span></div>';
        elements.chatBox.appendChild(indicator);
        scrollToBottom();
        return indicator;
    }

    function hideTypingIndicator(indicator) {
        if (indicator && indicator.parentNode) {
            indicator.remove();
        }
        state.isAgentTyping = false;
    }

    // ================= SEND MESSAGE =================
    function sendMessage() {
        const text = elements.chatInput?.value.trim();
        if (!text) return;
        
        const mode = state.currentMode;
        
        addMessage(mode, 'user', text);
        elements.chatInput.value = '';
        
        if (mode === 'bot') {
            handleBotResponse(text);
        } else {
            handleAgentResponse(text);
        }
    }

    function handleBotResponse(userText) {
        const indicator = showTypingIndicator();
        
        setTimeout(() => {
            hideTypingIndicator(indicator);
            
            let reply = "Sorry, I didn't understand that. Try selecting a category above or ask about orders, payments, or account help.";
            const lower = userText.toLowerCase();
            
            if (lower.includes('track')) reply = CONFIG.botResponses.tracking;
            else if (lower.includes('order')) reply = CONFIG.botResponses.order;
            else if (lower.includes('payment') || lower.includes('billing') || lower.includes('card')) 
                reply = CONFIG.botResponses.billing;
            else if (lower.includes('account') || lower.includes('password') || lower.includes('login')) 
                reply = CONFIG.botResponses.account;
            else if (lower.includes('refund') || lower.includes('return')) 
                reply = CONFIG.botResponses.returns;
            else if (lower.includes('bug') || lower.includes('error') || lower.includes('crash')) 
                reply = CONFIG.botResponses.technical;
            else if (lower.includes('contact') || lower.includes('email') || lower.includes('support')) 
                reply = CONFIG.botResponses.contact;
            
            addMessage('bot', 'bot', reply);
        }, CONFIG.botTypingDelay);
    }

    function handleAgentResponse(userText) {
        const indicator = showTypingIndicator();
        
        setTimeout(() => {
            hideTypingIndicator(indicator);
            const agentReply = generateAgentReply(userText);
            addMessage('agent', 'agent', agentReply);
        }, CONFIG.agentTypingDelay);
    }

    function generateAgentReply(userText) {
        const lower = userText.toLowerCase();
        
        if (lower.includes('order') || lower.includes('tracking')) {
            return "I can help with that! Could you provide your order number so I can look it up?";
        } else if (lower.includes('refund') || lower.includes('return')) {
            return "I'd be happy to help with your return. What's the reason for the return?";
        } else if (lower.includes('payment') || lower.includes('billing')) {
            return "Let me check your payment details. Can you confirm the email on your account?";
        } else if (lower.includes('hello') || lower.includes('hi')) {
            return "Hello! 👋 How can I assist you today?";
        } else {
            return "Thanks for your message. A specialist will review this and get back to you shortly. Is there anything else I can help with right now?";
        }
    }

    // ================= QUICK SOLUTIONS =================
    function bindQuickSolutions() {
        document.querySelectorAll('.quick-solution-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                const message = e.currentTarget.dataset.message;
                if (!message) return;
                
                if (elements.chatInput) {
                    elements.chatInput.value = message;
                }
                sendMessage();
                
                btn.classList.add('active');
                setTimeout(() => btn.classList.remove('active'), 200);
            });
        });
    }

    // ================= QUICK SOLUTIONS TOGGLE - FIXED =================
    function initQuickSolutionsToggle() {
        const toggleBtn = elements.toggleQuickSolutions;
        const solutionsContainer = elements.quickSolutions;
        
        if (!toggleBtn || !solutionsContainer) return;
        
        // Load saved preference
        const isCollapsed = localStorage.getItem('quickSolutionsCollapsed') === 'true';
        if (isCollapsed) {
            solutionsContainer.classList.add('collapsed');
            toggleBtn.innerHTML = '<span>Show</span><i class="fas fa-chevron-down"></i>';
            toggleBtn.title = 'Show quick solutions';
        }
        
        toggleBtn.addEventListener('click', () => {
            solutionsContainer.classList.toggle('collapsed');
            const isNowCollapsed = solutionsContainer.classList.contains('collapsed');
            
            if (isNowCollapsed) {
                toggleBtn.innerHTML = '<span>Show</span><i class="fas fa-chevron-down"></i>';
                toggleBtn.title = 'Show quick solutions';
            } else {
                toggleBtn.innerHTML = '<span>Hide</span><i class="fas fa-chevron-up"></i>';
                toggleBtn.title = 'Hide quick solutions';
            }
            
            localStorage.setItem('quickSolutionsCollapsed', isNowCollapsed);
        });
    }

    // ================= EVENT LISTENERS =================
    function bindEvents() {
        // Mode toggle buttons
        elements.modeBtns.forEach(btn => {
            btn.addEventListener('click', (e) => {
                const mode = e.currentTarget.dataset.mode;
                switchMode(mode);
            });
        });

        // Clear button
        if (elements.clearBtn) {
            elements.clearBtn.addEventListener('click', () => {
                clearConversation(state.currentMode);
            });
        }

        // Send button
        if (elements.sendBtn && elements.chatInput) {
            elements.sendBtn.addEventListener('click', sendMessage);
            elements.chatInput.addEventListener('keypress', (e) => {
                if (e.key === 'Enter') sendMessage();
            });
        }

        // Help card clicks (for bot mode quick responses)
        document.querySelectorAll('.help-card').forEach(card => {
            card.addEventListener('click', (e) => {
                if (e.target.closest('.help-card-view')) return;
                
                if (state.currentMode !== 'bot') {
                    switchMode('bot');
                }
                
                const type = card.getAttribute('data-help');
                const title = card.querySelector('h3')?.innerText || 'Help';
                
                addMessage('bot', 'user', title);
                handleBotResponse(title);
            });
        });
    }

    // ================= FAQ ACCORDION -> CHAT =================
    function bindFAQToChat() {
        document.querySelectorAll('.faq-question').forEach(q => {
            q.addEventListener('click', () => {
                const item = q.parentElement;
                if (!item) return;
                
                document.querySelectorAll('.faq-item').forEach(i => {
                    if (i !== item) i.classList.remove('active');
                });
                item.classList.toggle('active');

                const question = q.children[1]?.innerText || '';
                const answer = item.querySelector('.faq-answer')?.innerText || '';

                if (state.currentMode !== 'bot') {
                    switchMode('bot');
                }

                addMessage('bot', 'user', question);
                const indicator = showTypingIndicator();
                
                setTimeout(() => {
                    hideTypingIndicator(indicator);
                    addMessage('bot', 'bot', answer);
                }, 600);
            });
        });
    }

    // ================= INIT =================
    function init() {
        initElements();
        
        if (!elements.chatBox) {
            console.warn('Chat component: Chat box not found');
            return;
        }
        
        bindEvents();
        bindFAQToChat();
        bindQuickSolutions();
        initQuickSolutionsToggle(); // ✅ Toggle init here
        
        renderConversation('bot');
        console.log('Chat component initialized');
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose API for external use
    window.ChatComponent = {
        switchMode,
        addMessage: (mode, type, text) => addMessage(mode, type, text),
        clearConversation: (mode) => clearConversation(mode),
        getCurrentMode: () => state.currentMode
    };

})();