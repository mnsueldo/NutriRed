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
        }, 6000);
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

/**
 * ==========================================================================
 * Gestor Global de Experiencia de Usuario (NutriRedUI)
 * - Toasts dinámicos Bootstrap 5 (reemplaza alert() nativos)
 * - Modal Universal de Confirmación (reemplaza confirm() nativos)
 * - Buscador Global Rápido / Command Palette (Ctrl + K)
 * ==========================================================================
 */
const NutriRedUI = {
    pendingConfirmForm: null,

    init: function () {
        this.initConfirmModals();
        this.initCommandPalette();
    },

    /**
     * Muestra una notificación Toast flotante de Bootstrap 5
     * @param {string} message - Texto del mensaje
     * @param {'success'|'danger'|'warning'|'info'} variant - Estilo visual
     * @param {string} [title] - Título opcional
     */
    toast: function (message, variant = 'warning', title = '') {
        const container = document.getElementById('toastContainerGlobal');
        if (!container) return;

        const config = {
            success: { icon: 'bi-check-circle-fill text-success', border: 'border-success', defaultTitle: 'Operación exitosa' },
            danger:  { icon: 'bi-exclamation-octagon-fill text-danger', border: 'border-danger', defaultTitle: 'Atención requerida' },
            warning: { icon: 'bi-exclamation-triangle-fill text-warning', border: 'border-warning', defaultTitle: 'Validación' },
            info:    { icon: 'bi-info-circle-fill text-info', border: 'border-info', defaultTitle: 'Información' }
        };

        const cfg = config[variant] || config.warning;
        const headerTitle = title || cfg.defaultTitle;

        const toastEl = document.createElement('div');
        toastEl.className = `toast align-items-center bg-white border-0 border-start border-4 ${cfg.border} shadow-lg rounded-3 mb-2`;
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');

        toastEl.innerHTML = `
            <div class="d-flex p-3 align-items-start">
                <i class="bi ${cfg.icon} fs-5 me-2 mt-0"></i>
                <div class="me-auto pe-2">
                    <strong class="d-block text-dark small">${headerTitle}</strong>
                    <div class="text-secondary small mt-0.5">${message}</div>
                </div>
                <button type="button" class="btn-close ms-2 mt-1" data-bs-dismiss="toast" aria-label="Cerrar"></button>
            </div>
        `;

        container.appendChild(toastEl);
        const bsToast = new bootstrap.Toast(toastEl, { delay: 4500 });
        bsToast.show();

        toastEl.addEventListener('hidden.bs.toast', function () {
            toastEl.remove();
        });
    },

    /**
     * Intercepta formularios con atributo data-confirm-message y abre #modalConfirmacionUniversal
     */
    initConfirmModals: function () {
        const self = this;
        const modalEl = document.getElementById('modalConfirmacionUniversal');
        if (!modalEl) return;

        const titleEl = document.getElementById('modalConfirmacionTitle');
        const msgEl = document.getElementById('modalConfirmMessage');
        const iconBox = document.getElementById('modalConfirmIconBox');
        const iconEl = document.getElementById('modalConfirmIcon');
        const btnAccept = document.getElementById('btnConfirmModalAccept');

        document.addEventListener('submit', function (e) {
            const form = e.target;
            if (!form || !form.hasAttribute('data-confirm-message')) return;
            if (form.dataset.confirmed === 'true') return;

            e.preventDefault();
            self.pendingConfirmForm = form;

            const title = form.getAttribute('data-confirm-title') || 'Confirmar Acción';
            const message = form.getAttribute('data-confirm-message') || '¿Está seguro de que desea continuar?';
            const variant = form.getAttribute('data-confirm-variant') || 'danger';
            const btnText = form.getAttribute('data-confirm-btn') || 'Confirmar';

            if (titleEl) titleEl.textContent = title;
            if (msgEl) msgEl.textContent = message;

            if (iconBox && iconEl) {
                iconBox.className = `bg-${variant} bg-opacity-10 text-${variant} p-2 rounded-3 d-flex align-items-center justify-content-center`;
                iconEl.className = variant === 'success'
                    ? 'bi bi-check-circle-fill fs-5'
                    : (variant === 'danger' ? 'bi bi-trash3-fill fs-5' : 'bi bi-exclamation-triangle-fill fs-5');
            }

            if (btnAccept) {
                btnAccept.className = `btn btn-${variant} rounded-pill px-4 fw-semibold shadow-sm`;
                btnAccept.innerHTML = `<i class="bi bi-check2-circle me-1"></i> ${btnText}`;
            }

            const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
            bsModal.show();
        });

        if (btnAccept) {
            btnAccept.addEventListener('click', function () {
                if (self.pendingConfirmForm) {
                    self.pendingConfirmForm.dataset.confirmed = 'true';
                    btnAccept.disabled = true;
                    btnAccept.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Procesando...';
                    self.pendingConfirmForm.submit();
                }
            });
        }
    },

    /**
     * Inicializa el Buscador Rápido / Command Palette (Ctrl + K)
     */
    initCommandPalette: function () {
        const modalEl = document.getElementById('modalCommandPalette');
        const inputEl = document.getElementById('inputCommandPalette');
        if (!modalEl || !inputEl) return;

        const items = modalEl.querySelectorAll('.cmd-item');
        const directBox = document.getElementById('cmdDirectSearchBox');
        const termTxt = document.getElementById('cmdSearchTermTxt');
        const linkTrazabilidad = document.getElementById('cmdLinkTrazabilidad');
        const linkFamilias = document.getElementById('cmdLinkFamilias');
        const linkProductos = document.getElementById('cmdLinkProductos');

        // Atajo global Ctrl + K
        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                const bsModal = bootstrap.Modal.getOrCreateInstance(modalEl);
                bsModal.show();
            }
        });

        modalEl.addEventListener('shown.bs.modal', function () {
            inputEl.value = '';
            inputEl.dispatchEvent(new Event('input'));
            inputEl.focus();
        });

        inputEl.addEventListener('input', function () {
            const q = this.value.trim();
            const qLower = q.toLowerCase();

            items.forEach(item => {
                const text = (item.textContent || '').toLowerCase();
                const kw = (item.getAttribute('data-keywords') || '').toLowerCase();
                const match = !qLower || text.includes(qLower) || kw.includes(qLower);
                item.classList.toggle('d-none', !match);
            });

            if (q.length >= 2 && directBox) {
                directBox.classList.remove('d-none');
                if (termTxt) termTxt.textContent = `"${q}"`;
                const encoded = encodeURIComponent(q);
                if (linkTrazabilidad) linkTrazabilidad.href = `/Trazabilidad?busqueda=${encoded}`;
                if (linkFamilias) linkFamilias.href = `/Familias?busqueda=${encoded}`;
                if (linkProductos) linkProductos.href = `/Productos?busqueda=${encoded}`;
            } else if (directBox) {
                directBox.classList.add('d-none');
            }
        });

        inputEl.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const firstVisible = modalEl.querySelector('.cmd-item:not(.d-none)');
                const q = this.value.trim();
                if (firstVisible && q.length < 4) {
                    window.location.href = firstVisible.getAttribute('href');
                } else if (q.length >= 2 && linkTrazabilidad) {
                    window.location.href = linkTrazabilidad.href;
                }
            }
        });
    }
};

window.NutriRedUI = NutriRedUI;
document.addEventListener('DOMContentLoaded', function () {
    NutriRedUI.init();
});

