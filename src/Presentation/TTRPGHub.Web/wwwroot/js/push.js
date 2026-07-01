function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    return Uint8Array.from([...rawData].map(c => c.charCodeAt(0)));
}

window.pushNotifications = {
    isSupported: function () {
        return 'serviceWorker' in navigator && 'PushManager' in window;
    },

    requestPermissionAndSubscribe: async function (vapidPublicKey) {
        if (!this.isSupported()) return null;

        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return null;

        const registration = await navigator.serviceWorker.ready;
        let subscription = await registration.pushManager.getSubscription();

        if (!subscription) {
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
            });
        }

        const json = subscription.toJSON();
        return {
            endpoint: json.endpoint,
            p256dh: json.keys.p256dh,
            auth: json.keys.auth
        };
    },

    getCurrentSubscription: async function () {
        if (!this.isSupported()) return null;
        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();
        if (!subscription) return null;
        const json = subscription.toJSON();
        return { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth };
    },

    unsubscribe: async function () {
        if (!this.isSupported()) return null;
        const registration = await navigator.serviceWorker.ready;
        const subscription = await registration.pushManager.getSubscription();
        if (!subscription) return null;
        const json = subscription.toJSON();
        await subscription.unsubscribe();
        return { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth };
    }
};
