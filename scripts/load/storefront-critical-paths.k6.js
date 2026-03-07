import http from "k6/http";
import { check, sleep } from "k6";

export const options = {
  scenarios: {
    storefront_reads: {
      executor: "constant-vus",
      vus: 10,
      duration: "1m",
    },
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],
    http_req_duration: ["p(95)<600"],
  },
};

const appBaseUrl = __ENV.APPHOST_BASE_URL || "http://localhost:8080";
const storefrontBaseUrl = __ENV.STOREFRONT_BASE_URL || "http://localhost:5100";

export default function () {
  const responses = [
    http.get(`${storefrontBaseUrl}/`),
    http.get(`${storefrontBaseUrl}/product/mechanical-keyboard`),
    http.get(`${storefrontBaseUrl}/category/keyboards?page=1`),
    http.get(`${storefrontBaseUrl}/search?q=keyboard`),
    http.get(`${appBaseUrl}/api/v1/search/suggest?q=key&limit=8`),
  ];

  responses.forEach((response) => {
    check(response, {
      "status is acceptable": (r) => r.status >= 200 && r.status < 500,
    });
  });

  sleep(1);
}
