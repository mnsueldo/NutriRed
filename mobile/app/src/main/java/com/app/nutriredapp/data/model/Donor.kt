package com.app.nutriredapp.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
enum class DonorType {
    INDIVIDUAL,
    INSTITUTION,
    ANONYMOUS
}

@Serializable
data class Donor(
    val id: String? = null,
    val type: DonorType,
    @SerialName("document_number")
    val documentNumber: String? = null, // DNI o CUIT
    val name: String? = null,           // Nombre o Razón Social
    val phone: String? = null,
    val email: String? = null
)
