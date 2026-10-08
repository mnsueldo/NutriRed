package com.app.nutriredapp.di

import io.github.jan.supabase.SupabaseClient
import io.github.jan.supabase.auth.Auth
import io.github.jan.supabase.createSupabaseClient
import io.github.jan.supabase.postgrest.Postgrest
import io.ktor.client.engine.okhttp.OkHttp

object SupabaseModule {
    // Project credentials for Supabase
    private const val SUPABASE_URL = "https://oevedqotvxnsalasjmmf.supabase.co"
    private const val SUPABASE_ANON_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Im9ldmVkcW90dnhuc2FsYXNqbW1mIiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODk1NjY4OTAsImV4cCI6MjEwNTE0Mjg5MH0.nLb-4YdSPh4H0VKUNqkZB79Nlqz1lXYpggJWmlT8-UA"

    val client: SupabaseClient by lazy {
        createSupabaseClient(
            supabaseUrl = SUPABASE_URL,
            supabaseKey = SUPABASE_ANON_KEY
        ) {
            httpEngine = OkHttp.create()
            install(Auth)
            install(Postgrest)
        }
    }
}
