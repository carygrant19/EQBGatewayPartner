<template>
  <div class="min-vh-100 bg-light d-flex flex-column">
    <nav class="navbar navbar-expand-lg navbar-dark bg-dark shadow-sm px-4">
      <div class="container-fluid">
        <a class="navbar-brand d-flex align-items-center" href="#">
          <span class="fw-bold text-danger me-1">EQB</span> Gateway Portal
        </a>

        <div class="d-flex align-items-center">
          <span class="navbar-text text-white me-3 d-none d-sm-inline">
            Welcome, <strong>{{ username || "Portal User" }}</strong>
          </span>
          <button @click="handleSignOut" class="btn btn-outline-danger btn-sm">Sign Out</button>
        </div>
      </div>
    </nav>

    <div class="container-fluid flex-grow-1 my-4">
      <div class="row">
        <main class="col-12 px-md-4">
          <div
            class="d-flex justify-content-between flex-wrap flex-md-nowrap align-items-center pt-3 pb-2 mb-3 border-bottom"
          >
            <h1 class="h2 text-secondary">Dashboard Overview</h1>
          </div>

          <div class="row g-4 mb-4">
            <div class="col-md-4">
              <div class="card shadow-sm border-0 h-100 p-3">
                <h5 class="text-muted small text-uppercase font-weight-bold">
                  Active User Identity
                </h5>
                <h3 class="my-2">{{ username }}</h3>
                <span class="text-success small">Authenticated Session State Active</span>
              </div>
            </div>
            <div class="col-md-8">
              <div class="card shadow-sm border-0 h-100 p-3">
                <h5 class="text-muted small text-uppercase font-weight-bold">
                  System Announcements
                </h5>
                <p class="mt-2 text-secondary mb-0">
                  Welcome to the migrated Equicom Savings Bank Gateway Web application interface.
                  Use this console framework node array layout structure configuration layer for
                  transaction data pipelines.
                </p>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>

    <footer class="text-center py-3 bg-white border-top text-muted small mt-auto">
      &copy; {{ currentYear }} <span class="text-danger fw-bold">Equicom Savings Bank</span> All
      Rights Reserved.
    </footer>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";

const router = useRouter();
const username = ref("");
const currentYear = new Date().getFullYear();

// Interrogate session context verification payload rules
onMounted(async () => {
  try {
    // ⚠️ Points to the [HttpGet("status")] endpoint inside AuthController.cs
    const response = await axios.get("https://localhost:7176/api/auth/status", {
      withCredentials: true, // Forces browser to pass back your ASP.NET Core session/auth cookies
    });

    if (response.data && response.data.isAuthenticated) {
      username.value = response.data.username;
    } else {
      // Re-route unauthorized intercept targets instantly
      router.push("/");
    }
  } catch (error) {
    console.error("Session clearance exception context error:", error);
    router.push("/");
  }
});

function handleSignOut() {
  // Purge internal tracking keys
  localStorage.removeItem("authToken");
  router.push("/");
}
</script>
