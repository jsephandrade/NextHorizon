document.addEventListener("DOMContentLoaded", function () {

    const modal = document.getElementById("agentChatModal");
    const openBtn = document.getElementById("openAgentChat");
    const closeBtn = document.getElementById("closeAgentChat");
    const sendBtn = document.getElementById("sendAgentMessage");
    const input = document.getElementById("agentInput");
    const chatBody = document.getElementById("agentChatBody");

    let agentOnline = true;

    openBtn.addEventListener("click", () => {
        modal.style.display = "flex";
    });

    closeBtn.addEventListener("click", () => {
        modal.style.display = "none";
    });

    sendBtn.addEventListener("click", sendMessage);

    input.addEventListener("keypress", function(e){
        if(e.key === "Enter") sendMessage();
    });

    function sendMessage() {
        const msg = input.value.trim();
        if (!msg) return;

        chatBody.innerHTML += `<div class="chat-message user">${msg}</div>`;
        input.value = "";

        setTimeout(() => {
            chatBody.innerHTML += `
                <div class="chat-message bot">
                    Agent: We received your concern. Please wait...
                </div>
            `;
            chatBody.scrollTop = chatBody.scrollHeight;
        }, 1000);
    }

    function updateAgentStatus() {
        const statusText = document.getElementById("agentStatusText");
        const statusDot = document.getElementById("agentStatusDot");

        if (agentOnline) {
            statusText.textContent = "Online";
            statusText.style.color = "#00c853";
            statusDot.classList.add("online");
            statusDot.classList.remove("offline");
        } else {
            statusText.textContent = "Offline";
            statusText.style.color = "#d32f2f";
            statusDot.classList.add("offline");
            statusDot.classList.remove("online");
        }
    }

    updateAgentStatus();

});