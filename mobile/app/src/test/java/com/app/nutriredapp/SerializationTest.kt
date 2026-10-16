package com.app.nutriredapp

import com.app.nutriredapp.data.model.FoodPackage
import kotlinx.serialization.json.Json
import org.junit.Assert.*
import org.junit.Test
import java.io.InputStreamReader

class SerializationTest {

    private val json = Json {
        ignoreUnknownKeys = true
        coerceInputValues = true
        isLenient = true
        encodeDefaults = true
    }

    @Test
    fun testDeserializeSinglePackage() {
        val stream = javaClass.classLoader?.getResourceAsStream("sample_package.json")
        assertNotNull("sample_package.json not found", stream)
        val text = stream!!.bufferedReader().use { it.readText() }
        println("Testing deserialization of single package...")
        val pkg = json.decodeFromString<FoodPackage>(text)
        assertNotNull(pkg)
        assertEquals("PKG-2026-00002", pkg.packageCode)
        assertTrue(pkg.items.isNotEmpty())
        println("Success! Package code: ${pkg.packageCode}, items count: ${pkg.items.size}")
    }

    @Test
    fun testDeserializePreparedPackagesList() {
        val stream = javaClass.classLoader?.getResourceAsStream("sample_prepared_list.json")
        assertNotNull("sample_prepared_list.json not found", stream)
        val text = stream!!.bufferedReader().use { it.readText() }
        println("Testing deserialization of prepared packages list...")
        val list = json.decodeFromString<List<FoodPackage>>(text)
        assertNotNull(list)
        assertTrue(list.isNotEmpty())
        println("Success! List count: ${list.size}")
    }
}
