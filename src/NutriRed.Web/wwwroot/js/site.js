/**
 * ==========================================================================
 * NutriRed - Script Principal & Módulo de Accesibilidad Universal (WCAG 2.1)
 * Archivo: wwwroot/js/site.js
 * ==========================================================================
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Inicialización y persistencia del Módulo Integral de Accesibilidad
    NutriRedA11y.init();

    // 2. Cierre automático suave de alertas TempData luego de 6 segundos
    initAutoDismissAlerts();

    // 3. Inicialización de Tooltips de Bootstrap
    initTooltips();
});

/**
 * Gestor Centralizado de Accesibilidad de NutriRed
 */
const NutriRedA11y = {
    // Claves de Almacenamiento Local
    STORAGE_KEYS: {
        THEME: 'nutrired-a11y-theme',           // 'normal' | 'dark' | 'contrast'
        FONT_SIZE: 'nutrired-a11y-fontsize',   // 'sm' | 'md' | 'lg' | 'xl'
        DYSLEXIA: 'nutrired-a11y-dyslexia',     // 'true' | 'false'
        LINKS: 'nutrired-a11y-links',           // 'true' | 'false'
        MOTION: 'nutrired-a11y-motion'          // 'true' | 'false'
    },

    // Estado en memoria
    state: {
        theme: 'normal',
        fontSize: 'md',
        dyslexia: false,
        links: false,
        motion: false
    },

    init: function () {
        this.loadSavedPreferences();
        this.applyAll();
        this.bindEvents();
        this.bindKeyboardShortcuts();
    },

    /**
     * Recupera las preferencias guardadas en localStorage
     */
    loadSavedPreferences: function () {
        // Compatibilidad con versión previa de alto contraste
        const legacyContrast = localStorage.getItem('nutrired-high-contrast');
        if (legacyContrast === 'true') {
            this.state.theme = 'contrast';
            localStorage.setItem(this.STORAGE_KEYS.THEME, 'contrast');
            localStorage.removeItem('nutrired-high-contrast');
        } else {
            this.state.theme = localStorage.getItem(this.STORAGE_KEYS.THEME) || 'normal';
        }

        this.state.fontSize = localStorage.getItem(this.STORAGE_KEYS.FONT_SIZE) || 'md';
        this.state.dyslexia = localStorage.getItem(this.STORAGE_KEYS.DYSLEXIA) === 'true';
        this.state.links = localStorage.getItem(this.STORAGE_KEYS.LINKS) === 'true';
        this.state.motion = localStorage.getItem(this.STORAGE_KEYS.MOTION) === 'true';
    },

    /**
     * Aplica el estado completo al DOM y a los controles del Offcanvas
     */
    applyAll: function () {
        this.applyTheme(this.state.theme, false);
        this.applyFontSize(this.state.fontSize, false);
        this.applyDyslexia(this.state.dyslexia, false);
        this.applyLinks(this.state.links, false);
        this.applyMotion(this.state.motion, false);
    },

    /**
     * Modos de Color / Contraste: 'normal' | 'dark' | 'contrast'
     */
    applyTheme: function (theme, save = true) {
        this.state.theme = theme;
        if (save) localStorage.setItem(this.STORAGE_KEYS.THEME, theme);

        document.body.classList.remove('dark-mode', 'high-contrast');
        if (theme === 'dark') {
            document.body.classList.add('dark-mode');
        } else if (theme === 'contrast') {
            document.body.classList.add('high-contrast');
        }

        // Actualizar UI del panel si está presente
        document.querySelectorAll('.theme-option-btn').forEach(btn => {
            const isCurrent = btn.getAttribute('data-theme') === theme;
            btn.classList.toggle('active', isCurrent);
            btn.classList.toggle('border-primary', isCurrent && theme !== 'contrast');
            btn.classList.toggle('border-warning', isCurrent && theme === 'contrast');

            const check = btn.querySelector('.theme-check');
            if (check) {
                check.classList.toggle('d-none', !isCurrent);
            }
        });
    },

    /**
     * Tamaño de Fuente: 'sm' (90%) | 'md' (100%) | 'lg' (115%) | 'xl' (130%)
     */
    applyFontSize: function (size, save = true) {
        this.state.fontSize = size;
        if (save) localStorage.setItem(this.STORAGE_KEYS.FONT_SIZE, size);

        const html = document.documentElement;
        html.classList.remove('font-size-sm', 'font-size-md', 'font-size-lg', 'font-size-xl');
        html.classList.add('font-size-' + size);

        // Actualizar botones y badge de texto
        const sizeLabels = {
            'sm': 'Pequeño (90%)',
            'md': 'Normal (100%)',
            'lg': 'Grande (115%)',
            'xl': 'Muy grande (130%)'
        };

        const lbl = document.getElementById('lblFontSizeCurrent');
        if (lbl) lbl.textContent = sizeLabels[size] || 'Normal (100%)';

        const btnMap = {
            'sm': document.getElementById('btnFontSm'),
            'md': document.getElementById('btnFontMd'),
            'lg': document.getElementById('btnFontLg'),
            'xl': document.getElementById('btnFontXl')
        };

        Object.keys(btnMap).forEach(key => {
            if (btnMap[key]) {
                btnMap[key].classList.toggle('active', key === size);
                btnMap[key].classList.toggle('btn-primary', key === size);
                btnMap[key].classList.toggle('btn-outline-secondary', key !== size);
            }
        });
    },

    /**
     * Tipografía Apta para Dislexia
     */
    applyDyslexia: function (enabled, save = true) {
        this.state.dyslexia = enabled;
        if (save) localStorage.setItem(this.STORAGE_KEYS.DYSLEXIA, enabled ? 'true' : 'false');

        document.body.classList.toggle('dyslexia-font', enabled);

        const sw = document.getElementById('switchDyslexiaFont');
        if (sw) sw.checked = enabled;
    },

    /**
     * Resaltado y Subrayado de Enlaces
     */
    applyLinks: function (enabled, save = true) {
        this.state.links = enabled;
        if (save) localStorage.setItem(this.STORAGE_KEYS.LINKS, enabled ? 'true' : 'false');

        document.body.classList.toggle('highlight-links', enabled);

        const sw = document.getElementById('switchHighlightLinks');
        if (sw) sw.checked = enabled;
    },

    /**
     * Pausar Movimiento y Animaciones
     */
    applyMotion: function (enabled, save = true) {
        this.state.motion = enabled;
        if (save) localStorage.setItem(this.STORAGE_KEYS.MOTION, enabled ? 'true' : 'false');

        document.body.classList.toggle('reduced-motion', enabled);

        const sw = document.getElementById('switchReducedMotion');
        if (sw) sw.checked = enabled;
    },

    /**
     * Restablece todas las opciones a los valores por defecto del sistema
     */
    resetDefaults: function () {
        Object.values(this.STORAGE_KEYS).forEach(k => localStorage.removeItem(k));
        localStorage.removeItem('nutrired-high-contrast');

        this.state = {
            theme: 'normal',
            fontSize: 'md',
            dyslexia: false,
            links: false,
            motion: false
        };

        this.applyAll();
    },

    /**
     * Asocia los eventos de la interfaz del Panel Offcanvas
     */
    bindEvents: function () {
        const self = this;

        // Botones de temas
        document.querySelectorAll('.theme-option-btn').forEach(btn => {
            btn.addEventListener('click', function () {
                const theme = this.getAttribute('data-theme');
                self.applyTheme(theme, true);
            });
        });

        // Botones de tamaño de fuente
        const fontBtns = {
            'btnFontSm': 'sm',
            'btnFontMd': 'md',
            'btnFontLg': 'lg',
            'btnFontXl': 'xl'
        };

        Object.keys(fontBtns).forEach(id => {
            const el = document.getElementById(id);
            if (el) {
                el.addEventListener('click', function () {
                    self.applyFontSize(fontBtns[id], true);
                });
            }
        });

        // Switches de lectura y movimiento
        const swDyslexia = document.getElementById('switchDyslexiaFont');
        if (swDyslexia) {
            swDyslexia.addEventListener('change', function () {
                self.applyDyslexia(this.checked, true);
            });
        }

        const swLinks = document.getElementById('switchHighlightLinks');
        if (swLinks) {
            swLinks.addEventListener('change', function () {
                self.applyLinks(this.checked, true);
            });
        }

        const swMotion = document.getElementById('switchReducedMotion');
        if (swMotion) {
            swMotion.addEventListener('change', function () {
                self.applyMotion(this.checked, true);
            });
        }

        // Botón Restablecer
        const btnReset = document.getElementById('btnResetA11y');
        if (btnReset) {
            btnReset.addEventListener('click', function () {
                self.resetDefaults();
            });
        }
    },

    /**
     * Atajos de teclado: Alt + A para alternar el Panel de Accesibilidad
     */
    bindKeyboardShortcuts: function () {
        document.addEventListener('keydown', function (e) {
            if (e.altKey && (e.key === 'a' || e.key === 'A')) {
                e.preventDefault();
                const panelEl = document.getElementById('panelAccesibilidad');
                if (panelEl) {
                    const bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(panelEl);
                    if (panelEl.classList.contains('show')) {
                        bsOffcanvas.hide();
                    } else {
                        bsOffcanvas.show();
                    }
                }
            }
        });
    }
};

/**
 * Permite que las alertas no invasivas se cierren solas
 */
function initAutoDismissAlerts() {
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(function (alertElement) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alertElement);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 7000);
    });
}

/**
 * Inicializa tooltips de Bootstrap
 */
function initTooltips() {
    const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });
}
