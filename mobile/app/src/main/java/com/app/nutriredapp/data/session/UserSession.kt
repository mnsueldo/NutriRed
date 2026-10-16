package com.app.nutriredapp.data.session

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

object UserSession {
    private val _userId = MutableStateFlow<String>("VOL-101")
    val userId: StateFlow<String> = _userId

    private val _userName = MutableStateFlow<String>("Voluntario")
    val userName: StateFlow<String> = _userName

    private val _userEmail = MutableStateFlow<String>("")
    val userEmail: StateFlow<String> = _userEmail

    fun setLoggedInUser(name: String, email: String = "", id: String? = null) {
        val cleanName = name.trim()
        val displayName = when {
            cleanName.isNotBlank() && !cleanName.contains("@") -> cleanName
            email.isNotBlank() -> email.substringBefore('@').replaceFirstChar { it.uppercase() }
            cleanName.isNotBlank() -> cleanName.substringBefore('@').replaceFirstChar { it.uppercase() }
            else -> "Voluntario"
        }
        _userName.value = displayName
        _userEmail.value = email.trim()
        if (!id.isNullOrBlank()) {
            _userId.value = id
        } else {
            _userId.value = "VOL-${displayName.take(4).uppercase()}"
        }
    }

    fun logout() {
        _userId.value = "VOL-101"
        _userName.value = "Voluntario"
        _userEmail.value = ""
    }
}
