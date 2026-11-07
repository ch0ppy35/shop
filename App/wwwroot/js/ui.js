// Native <dialog> helpers; Blazor can't call element methods directly
window.ui = {
    showModal: function (dialog) {
        if (dialog && !dialog.open) {
            dialog.showModal();
        }
    },

    close: function (dialog) {
        if (dialog && dialog.open) {
            dialog.close();
        }
    }
};
