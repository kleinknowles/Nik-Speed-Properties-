/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  async rewrites() {
    const configuredApi = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5188";
    const api = (/^https?:\/\//i.test(configuredApi) ? configuredApi : `https://${configuredApi}`).replace(/\/$/, "");
    return [{ source: "/api/:path*", destination: `${api}/api/:path*` }];
  },
};

export default nextConfig;
