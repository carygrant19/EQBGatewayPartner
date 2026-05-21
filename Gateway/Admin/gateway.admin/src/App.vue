<template>
  <div style="background-color: #f8f9fa; min-height: 100vh">
    <LoginComponent />
  </div>
</template>

<script setup>
// 1. Keep standard external npm packages active
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap/dist/js/bootstrap.bundle.min.js";

// 2. TEMPORARILY COMMENTED OUT - These local custom paths are the likely 500 culprits
// import '@/assets/css/icons.min.css';
// import '@/assets/css/app.min.css';
// import '@/assets/css/site.css';
// import '@/assets/css/login.css';

// 3. Keep the login layout component active
import LoginComponent from "./components/Login.vue";
</script>
