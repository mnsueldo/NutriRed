package com.app.nutriredapp.data.model

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

@Serializable
data class User(
    val id: String? = null,
    val email: String,
    @SerialName("full_name")
    val fullName: String = "",
    val role: UserRole = UserRole.VOLUNTEER,
    @SerialName("password_hash")
    val passwordHash: String? = null
)
