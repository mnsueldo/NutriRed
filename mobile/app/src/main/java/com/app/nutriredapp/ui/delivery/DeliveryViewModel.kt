package com.app.nutriredapp.ui.delivery

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.app.nutriredapp.data.model.FoodPackage
import com.app.nutriredapp.data.repository.DeliveryRepository
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

data class DeliveryUiState(
    val preparedPackages: List<FoodPackage> = emptyList(),
    val selectedPackage: FoodPackage? = null,
    val receiverDni: String = "",
    val receiverName: String = "",
    val hasSignature: Boolean = false,
    val signatureBase64: String? = null,
    val errorMessage: String? = null,
    val isLoading: Boolean = false,
    val isSubmitting: Boolean = false,
    val completedPackage: FoodPackage? = null,
    val failedDeliveryMessage: String? = null
)

class DeliveryViewModel(
    private val repository: DeliveryRepository = DeliveryRepository()
) : ViewModel() {

    private val _uiState = MutableStateFlow(DeliveryUiState())
    val uiState: StateFlow<DeliveryUiState> = _uiState.asStateFlow()

    init {
        loadPackages()
    }

    fun loadPackages() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, errorMessage = null) }
            val packages = repository.getPreparedPackages()
            _uiState.update { it.copy(preparedPackages = packages, isLoading = false) }
        }
    }

    fun selectPackage(pkg: FoodPackage) {
        _uiState.update {
            it.copy(
                selectedPackage = pkg,
                receiverDni = "",
                receiverName = pkg.familyTitularName, // Sugiere el nombre del titular para facilitar
                hasSignature = false,
                errorMessage = null,
                completedPackage = null
            )
        }
    }

    fun clearSelection() {
        _uiState.update {
            it.copy(
                selectedPackage = null,
                receiverDni = "",
                receiverName = "",
                hasSignature = false,
                errorMessage = null,
                completedPackage = null
            )
        }
    }

    fun onReceiverDniChanged(dni: String) {
        _uiState.update { it.copy(receiverDni = dni, errorMessage = null) }
    }

    fun onReceiverNameChanged(name: String) {
        _uiState.update { it.copy(receiverName = name, errorMessage = null) }
    }

    fun onSignatureChanged(hasSig: Boolean, base64: String? = null) {
        _uiState.update { it.copy(hasSignature = hasSig, signatureBase64 = base64, errorMessage = null) }
    }

    fun confirmDelivery() {
        val state = _uiState.value
        val pkg = state.selectedPackage ?: return

        // 1. Validar DNI
        if (state.receiverDni.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Ingrese el DNI de quien retira el paquete.") }
            return
        }

        // 2. Validar coincidencia de identidad con titular familiar (RF-3.5)
        if (state.receiverDni.trim() != pkg.familyTitularDni.trim()) {
            _uiState.update { 
                it.copy(errorMessage = "Discrepancia de identidad: El DNI ingresado no coincide con el titular registrado (${pkg.familyTitularDni}).") 
            }
            return
        }

        // 3. Validar nombre
        if (state.receiverName.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Ingrese el nombre completo del receptor.") }
            return
        }

        // 4. Validar firma digital (RF-3.5)
        if (!state.hasSignature) {
            _uiState.update { it.copy(errorMessage = "Firma digital vacía: Debe firmar en pantalla para confirmar.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null) }

            val result = repository.confirmDelivery(
                packageId = pkg.id,
                receiverDni = state.receiverDni,
                receiverName = state.receiverName,
                signatureBase64 = state.signatureBase64 ?: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAMgAAABkCAYAAADDhn8LAAAACXBIWXMAAAsTAAALEwEAmpwYAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAFTSURBVHhe7cExAQAAAMKg9U9tDQ8gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAgIsBFecAAafqH7MAAAAASUVORK5CYII=",
                volunteerId = "VOL-101",
                volunteerName = "Voluntario de Entrega"
            )

            result.onSuccess { delivered ->
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        completedPackage = delivered,
                        selectedPackage = null
                    )
                }
                loadPackages()
            }.onFailure { err ->
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        errorMessage = err.message ?: "Error al registrar la entrega."
                    )
                }
            }
        }
    }

    fun registerFailedDelivery(reason: String) {
        val state = _uiState.value
        val pkg = state.selectedPackage ?: return

        if (reason.isBlank()) {
            _uiState.update { it.copy(errorMessage = "Debe indicar el motivo por el cual no se concretó la entrega.") }
            return
        }

        viewModelScope.launch {
            _uiState.update { it.copy(isSubmitting = true, errorMessage = null) }

            val result = repository.registerFailedDelivery(
                packageId = pkg.id,
                packageCode = pkg.packageCode,
                reason = reason.trim(),
                volunteerId = "VOL-101",
                volunteerName = "Voluntario de Entrega"
            )

            result.onSuccess {
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        selectedPackage = null,
                        failedDeliveryMessage = "Paquete ${pkg.packageCode} asentado como NO ENTREGADO / CANCELADO ($reason).",
                        errorMessage = null
                    )
                }
                loadPackages()
            }.onFailure { err ->
                _uiState.update {
                    it.copy(
                        isSubmitting = false,
                        errorMessage = err.message ?: "Error al registrar la no concreción de la entrega."
                    )
                }
            }
        }
    }

    fun dismissFailedDeliveryMessage() {
        _uiState.update { it.copy(failedDeliveryMessage = null) }
    }
}
