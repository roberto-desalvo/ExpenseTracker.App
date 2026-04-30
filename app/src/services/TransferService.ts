import config from "../config/development";
import { TransferPayload } from "../models/Transfer";
import { apiFetchVoid } from "./ApiClient";

const TransferService = {
  add: async (transfer: TransferPayload): Promise<void> => {
    const url = `${config.expenseTrackerBaseUrl}/${config.expenseTrackerTransferUrl}`;
    await apiFetchVoid(
      url,
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
