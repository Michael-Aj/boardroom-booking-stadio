// wwwroot/js/loading-overlay.js
(() => {
    const overlay = document.getElementById('loading-overlay');
    if (!overlay) return;

    // ❶  Show on any <form> submit
    document.addEventListener('submit', e => {
        const form = e.target;
        if (form.matches('form')) overlay.classList.remove('d-none');
    });

    // ❷  Optional: show on any normal link click (skip target=_blank, hashes, etc.)
    document.addEventListener('click', e => {
        const link = e.target.closest('a[href]');
        if (!link) return;
        const href = link.getAttribute('href');
        const isExternal = /^https?:\/\//i.test(href) && !href.startsWith(location.origin);
        const newTab     = link.target === '_blank';
        if (!isExternal && !newTab && !href.startsWith('#'))
            overlay.classList.remove('d-none');
    });

    // ❸  Optional: hook fetch/ajax (needs Abortable fetch polyfill for older browsers)
    const { fetch: origFetch } = window;
    window.fetch = async (...args) => {
        overlay.classList.remove('d-none');
        try   { return await origFetch(...args); }
        finally { overlay.classList.add('d-none'); }
    };

    // ❹  Hide after navigation completes (for partial loads, Turbo, etc.)
    window.addEventListener('pageshow', () => overlay.classList.add('d-none'));
})();
