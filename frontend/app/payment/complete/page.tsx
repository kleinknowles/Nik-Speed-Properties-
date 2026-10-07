"use client";
import Link from "next/link";
import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { SiteShell } from "../../../components/SiteShell";

export default function PaymentComplete(){
  const params=useSearchParams();
  const[message,setMessage]=useState("Checking your payment status…");
  useEffect(()=>{
    const token=localStorage.getItem("nikspeed-token");
    const reference=params.get("tx_ref");
    const transactionId=params.get("transaction_id");
    if(!token||!reference||!transactionId){setMessage("We could not verify this payment. Check your payment history in the dashboard or contact our team.");return;}
    fetch(`/api/payments/verify?${new URLSearchParams({tx_ref:reference,transaction_id:transactionId})}`,{headers:{Authorization:`Bearer ${token}`}})
      .then(async response=>{const data=await response.json().catch(()=>null);if(!response.ok)throw new Error(data?.message||"We could not verify this payment.");setMessage(data?.status==="paid"?"Payment confirmed. Your listing credits are ready in the dashboard.":"Payment is still being confirmed. Your dashboard will update once it is verified.");})
      .catch(error=>setMessage(error.message||"We could not verify this payment. Please check the dashboard again shortly."));
  },[params]);
  return <SiteShell><main className="page empty"><p className="eyebrow">PAYMENT RETURN</p><h1>Payment status</h1><p role="status">{message}</p><Link className="button-link" href="/dashboard">Go to dashboard</Link></main></SiteShell>
}
