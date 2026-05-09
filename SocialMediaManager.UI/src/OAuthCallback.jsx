import React, { useEffect, useRef } from 'react';
import axios from 'axios';

export default function OAuthCallback() {
  const hasCalled = useRef(false);
  useEffect(() => {
    if (hasCalled.current) return;
    hasCalled.current = true;

    console.log("🔍 OAuthCallback useEffect triggered");
    const urlParams = new URLSearchParams(window.location.search);
    const code = urlParams.get('code');
    const state = urlParams.get('state');
    
    console.log("📍 URL params:", { code: code?.substring(0, 20) + "...", state });

    if (code) {
      const platform = state === 'linkedin_auth' ? 'linkedin' : 'x';
      console.log("🚀 Platform detected:", platform);

      console.log("📤 Sending code to backend:", code.substring(0, 20) + "...");
      (async () => {
        try {
          const response = await axios.post(`http://localhost:5195/api/oauth/${platform}/exchange`, { code });
          console.log("✅ Exchange response:", response.data);
          alert(`${platform.toUpperCase()} connected successfully!`);
          window.location.href = "/";
        } catch (err) {
          console.error("❌ OAuth Exchange Failed", err);
          alert("Failed to connect account.");
        }
      })();
    } else {
      console.warn("⚠️ No code found in URL");
    }
  }, []);

  return <div className="p-10 text-white">Connecting your account... Please wait.</div>;
}