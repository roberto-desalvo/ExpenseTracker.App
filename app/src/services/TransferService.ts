import { apiConfig } from "../config/api";
import { TransferPayload } from "../models/Transfer";
import { apiFetchVoid } from "./ApiClient";

const TransferService = {
  add: async (transfer: TransferPayload): Promise<void> => {
    await apiFetchVoid(
      apiConfig.transfers.base,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(transfer),
      },
      "Errore nel salvataggio del trasferimento"
    );
  },
};

export default TransferService;
