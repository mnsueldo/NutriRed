package com.app.nutriredapp.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
enum class PackageStatus {
    @SerialName("PREPARADO")
    PREPARADO,
    @SerialName("ENTREGADO")
    ENTREGADO
}

@Serializable
data class FoodPackage(
    val id: String,
    @SerialName("package_code")
    val packageCode: String, // ej. PAQ-2026-00012
    @SerialName("family_id")
    val familyId: String,
    @SerialName("family_titular_name")
    val familyTitularName: String,
    @SerialName("family_titular_dni")
    val familyTitularDni: String,
    @SerialName("family_members_count")
    val familyMembersCount: Int,
    val items: List<DonationItem> = emptyList(),
    val status: PackageStatus = PackageStatus.PREPARADO,
    @SerialName("volunteer_id")
    val volunteerId: String? = null,
    @SerialName("volunteer_name")
    val volunteerName: String? = null,
    @SerialName("delivered_at")
    val deliveredAt: String? = null,
    @SerialName("receiver_dni")
    val receiverDni: String? = null,
    @SerialName("receiver_name")
    val receiverName: String? = null,
    @SerialName("has_signature")
    val hasSignature: Boolean = false
)
