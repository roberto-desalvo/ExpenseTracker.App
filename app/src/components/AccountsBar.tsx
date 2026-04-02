import AccountBoxItem from "./AccountBoxItem";
import Account from "../models/Account";
import { Stack } from "@mui/material";
import { useAccounts } from "../stores/AccountContext";

export default function AccountsBar() {
  const accountsContext = useAccounts();
  return (
    <>
      <div className="h-1/8 text-white flex items-center pl-2 pr-6 py-4">
        <Stack direction="row" spacing={2} sx={{ width: "100%" }}>
          {accountsContext.accounts.map((account: Account) => (
            <AccountBoxItem key={account.id} account={account} />
          ))}
        </Stack>
      </div>
    </>
  );
}
