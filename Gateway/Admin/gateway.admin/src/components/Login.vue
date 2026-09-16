<template>
  <div class="d-flex flex-column min-vh-100 p-5 bg-light">
    <section class="flex-grow-1 d-flex justify-content-center align-items-center">
      <div class="container-fluid">
        <div class="row d-flex align-items-center justify-content-center">
          <div class="col-lg-4 col-md-6 col-sm-12">
            <div class="login-form bg-white p-4 rounded shadow-sm border">
              <div class="text-center mb-3">
                <h3 class="text-danger fw-bold">EQB Gateway</h3>
              </div>

              <h1 class="text-center h3 mb-2"><b>Sign in</b></h1>
              <p class="text-center text-muted small">
                Please sign in to access the <b>EQB Portal</b>
              </p>

              <form @submit.prevent="handleLogin" autocomplete="off">
                <div class="mb-3">
                  <label class="form-label text-secondary small fw-bold">Username</label>
                  <input v-model="form.username" type="text" class="form-control" required />
                </div>

                <div class="mb-3">
                  <label class="form-label text-secondary small fw-bold">Password</label>
                  <input
                    v-model="form.password"
                    type="password"
                    class="form-control"
                    placeholder="**********"
                    required
                  />
                </div>

                <button type="submit" class="btn btn-danger w-100 mt-2" :disabled="isLoading">
                  <span v-if="isLoading" class="spinner-border spinner-border-sm me-1"></span>
                  <span v-if="isLoading">SIGNING IN...</span>
                  <span v-else>LOGIN</span>
                </button>
              </form>

              <div
                v-if="errorMessage"
                class="alert alert-danger p-3 rounded-3 mt-3 small shadow-sm"
                role="alert"
              >
                <strong>Error: </strong>{{ errorMessage }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <footer class="text-center py-3 text-muted small">
      &copy; {{ currentYear }} <span class="text-danger fw-bold">Equicom Savings Bank</span> All
      Rights Reserved.
    </footer>
  </div>
</template>

<script setup>
import { ref, reactive } from "vue";
import { useRouter } from "vue-router";
import axios from "axios";
import Swal from "sweetalert2";

const router = useRouter();
const form = reactive({ username: "", password: "" });

const isLoading = ref(false);
const errorMessage = ref("");
const currentYear = new Date().getFullYear();

async function handleLogin() {
  isLoading.value = true;
  errorMessage.value = "";

  try {
    // ⚠️ Using your exact appsettings.json localhost port definition target configuration value
    const apiURL = "https://localhost:7274/api/auth/login";

    const response = await axios.post(
      apiURL,
      {
        Username: form.username,
        Password: form.password,
      },
      {
        withCredentials: true, // CRITICAL: This allows the .NET cookie framework engine to save state inside the browser
      },
    );

    // Check the distinct status response tokens from your AuthController execution rules
    const { status, redirectUrl } = response.data;

    if (status === "SUCCESS") {
      Swal.fire({
        icon: "success",
        title: "Welcome Back",
        text: "Access allowed. Initializing dashboard components...",
        timer: 1500,
        showConfirmButton: false,
      }).then(() => {
        router.push(redirectUrl); // Routes directly to /Home
      });
    } else if (status === "PASSWORD_EXPIRED" || status === "ACTIVESESSION") {
      // Safely route to intermediate verification buffers
      router.push(redirectUrl);
    }
  } catch (error) {
    console.error("API Context error execution path failure details:", error);
    if (error.response && error.response.data) {
      errorMessage.value =
        error.response.data.message || "Authentication rejected by security provider.";
    } else {
      errorMessage.value =
        "Connection tracking failed: Ensure your C# Visual Studio Solution is compiling and active.";
    }
  } finally {
    isLoading.value = false;
  }
}
</script>
