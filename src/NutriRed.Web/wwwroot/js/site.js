/**
 * NutriRed - Script Principal de Interfaz & Accesibilidad (site.js)
 */

document.addEventListener('DOMContentLoaded', function () {
    // 1. Inicialización y persistencia del Modo Alto Contraste (WCAG)
    initHighContrast();

    // 2. Cierre automático suave de alertas TempData luego de 6 segundos
    initAutoDismissAlerts();

    // 3. Inicialización de Tooltips de Bootstrap si existen
    initTooltips();
});

/**
 * Gestiona el modo de alto contraste para personas con disminución visual
 */
function initHighContrast() {
    const storageKey = 'nutrired-high-contrast';
    const toggleBtn = document.getElementById('btnToggleHighContrast');
    const isHighContrast = localStorage.getItem(storageKey) === 'true';

    // Aplicar estado inicial guardado
    if (isHighContrast) {
        document.body.classList.add('high-contrast');
        updateToggleButtonUI(true);
    }

    if (toggleBtn) {
        toggleBtn.addEventListener('click', function () {
            const active = document.body.classList.toggle('high-contrast');
            localStorage.setItem(storageKey, active ? 'true' : 'false');
            updateToggleButtonUI(active);
        });
    }
}

/**
 * Actualiza el icono y texto del botón de accesibilidad
 */
function updateToggleButtonUI(isActive) {
    const icon = document.getElementById('highContrastIcon');
    const label = document.getElementById('highContrastLabel');

    if (icon) {
        icon.className = isActive ? 'bi bi-eye-fill text-warning me-1' : 'bi bi-circle-half me-1';
    }
    if (label) {
        label.textContent = isActive ? 'Alto Contraste: ON' : 'Contraste';
    }
}

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
