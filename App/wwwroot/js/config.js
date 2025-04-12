// Configuration helper for Blazor app
window.configHelper = {
    // Get the API base URL from a global variable, environment, or default to the one in appsettings.json
    getApiBaseUrl: function () {
        // Check if there's a global variable set (useful for deployment scenarios)
        if (window.API_BASE_URL) {
            return window.API_BASE_URL;
        }

        // Default to null, which will make the app use the value from appsettings.json
        return null;
    },

    // Session management functions
    session: {
        // Get the session ID from local storage
        getSessionId: function () {
            return localStorage.getItem('nats_shop_session_id');
        },

        // Set the session ID in local storage
        setSessionId: function (sessionId) {
            localStorage.setItem('nats_shop_session_id', sessionId);
        },

        // Clear the session ID from local storage
        clearSessionId: function () {
            localStorage.removeItem('nats_shop_session_id');
        }
    }
};
