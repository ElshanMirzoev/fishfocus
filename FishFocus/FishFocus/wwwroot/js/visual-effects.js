window.visualEffects = {
    createRipple: function (selector, clientX, clientY) {
        const container = document.querySelector(selector);
        if (!container) return;

        const rect = container.getBoundingClientRect();
        
        const scaleX = container.offsetWidth > 0 ? (rect.width / container.offsetWidth) : 1.0;
        const scaleY = container.offsetHeight > 0 ? (rect.height / container.offsetHeight) : 1.0;

        const x = (clientX - rect.left) / scaleX;
        const y = (clientY - rect.top) / scaleY;

        const ripple = document.createElement("div");
        ripple.className = "click-ripple";

        ripple.style.left = `${x}px`;
        ripple.style.top = `${y}px`;

        container.appendChild(ripple);

        setTimeout(() => {
            ripple.remove();
        }, 1000);
    }
};