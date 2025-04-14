// Cart helper functions
window.cartHelper = {
    // Refresh the navigation menu to update the cart count
    refreshNavMenu: function() {
        // This is a workaround to force Blazor to re-render the NavMenu component
        // In a real application, you might use a state management solution or event aggregator
        var event = new CustomEvent('cartUpdated');
        document.dispatchEvent(event);
    }
};
