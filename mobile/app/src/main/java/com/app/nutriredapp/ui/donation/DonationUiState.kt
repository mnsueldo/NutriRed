package com.app.nutriredapp.ui.donation

import com.app.nutriredapp.data.model.Donation
import com.app.nutriredapp.data.model.DonationItem
import com.app.nutriredapp.data.model.Donor
import com.app.nutriredapp.data.model.DonorType
import com.app.nutriredapp.data.model.Product

enum class DonationStep(val stepNumber: Int, val title: String) {
    DONOR(1, "Identificación del Donante"),
    ITEMS(2, "Carga de Alimentos"),
    SUMMARY(3, "Revisión y Confirmación"),
    RECEIPT(4, "Comprobante Digital")
}

data class DonationUiState(
    val currentStep: DonationStep = DonationStep.DONOR,

    // Datos del Donante
    val donorType: DonorType = DonorType.INDIVIDUAL,
    val documentNumber: String = "",
    val donorName: String = "",
    val donorPhone: String = "",
    val donorEmail: String = "",
    val isDonorExisting: Boolean = false,
    val donorError: String? = null,

    // Carga de producto / lote en curso
    val currentBarcode: String = "",
    val currentProductName: String = "",
    val currentUnitOfMeasure: String = "unidades",
    val currentQuantity: String = "1",
    val currentExpirationMonth: String = "", // Formato MM (ej. 10)
    val currentExpirationYear: String = "",  // Formato AAAA (ej. 2026)
    val currentExpirationDate: String = "", // Formato MM/AAAA
    val currentBatchNumber: String = "",
    val isCameraScanning: Boolean = false,
    val isProductFromCatalog: Boolean = false,
    val itemErrorMessage: String? = null,

    // Lista de alimentos cargados en la donación actual
    val items: List<DonationItem> = emptyList(),

    // Catálogo sugerido / búsqueda
    val catalogProducts: List<Product> = emptyList(),
    val catalogQuery: String = "",

    // Estado de guardado y comprobante
    val isSubmitting: Boolean = false,
    val completedDonation: Donation? = null,
    val globalError: String? = null
)
