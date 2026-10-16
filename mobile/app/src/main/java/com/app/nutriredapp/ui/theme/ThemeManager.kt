package com.app.nutriredapp.ui.theme

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow

object ThemeManager {
    // null significa seguir la preferencia del sistema operativo
    private val _isDarkTheme = MutableStateFlow<Boolean?>(null)
    val isDarkTheme: StateFlow<Boolean?> = _isDarkTheme

    fun setDarkTheme(isDark: Boolean) {
        _isDarkTheme.value = isDark
    }

    fun toggle(currentIsDark: Boolean) {
        _isDarkTheme.value = !currentIsDark
    }
}
