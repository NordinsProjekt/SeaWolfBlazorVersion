// highScore.js — name prompt and highscore tab toggle bridge
window.SeaWolfHighScore = {
    promptName(score) {
        const raw = prompt(`New score: ${score}\nEnter your name (max 12 chars):`, 'ACE');
        if (raw === null) return 'UNKNOWN';
        return raw.trim().substring(0, 12) || 'UNKNOWN';
    },

    toggleHighScores() {
        if (window.SeaWolfRendererScreens)
            window.SeaWolfRendererScreens.toggleHighScores();
    },

    // Returns the hit-boxes drawn this frame (canvas coordinates).
    getStartScreenButtons() {
        if (window.SeaWolfRendererScreens)
            return window.SeaWolfRendererScreens.getStartScreenButtons();
        return null;
    }
};
