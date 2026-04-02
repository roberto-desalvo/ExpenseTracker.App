import { Typography } from "@mui/material";
import Account from "../models/Account.tsx";
import Stack from "@mui/material/Stack";
import { useState } from "react";
import { useTableContext } from "../stores/TableContext.tsx";

interface AccountBoxItemProps {
  account: Account;
}

export default function AccountBoxItem({ account }: AccountBoxItemProps) {
  const tableContext = useTableContext();

  const [isSelected, setIsSelected] = useState(
    tableContext.selectedAccounts.filter((x) => x.id == account.id).length > 0
  );

  const handleClick = () => {
    if (isSelected) {
      tableContext.removeFromSelectedAccount(account);
      setIsSelected(false);
    } else {
      tableContext.addToSelectedAccount(account);
      setIsSelected(true);
    }
  };

  const getAccountOutcome = () => {
    const filteredTransactions = tableContext.getFilteredTransactions();

    const accountTransactions = filteredTransactions.filter(
      (transaction) => transaction.accountId === account.id
    );

    const totalOutcome = accountTransactions.reduce(
      (acc, transaction) => acc + transaction.amount,
      0
    );

    return totalOutcome;
  };

  const typographyStyle = {
    flex: 1,
    padding: "2px 8px",
    fontWeight: 500,
    color: "#cddc39",
    textTransform: "uppercase",
  };

  return (
    <>
      <Stack
        className="flex-1 h-12 border"
        direction="row"
        spacing={2}
        sx={{
          alignItems: "center",
          justifyContent: "center",
        }}
        onClick={() => handleClick()}
      >
        <div className="flex w-full justify-between items-center">
          <Typography sx={typographyStyle}>{account.name}</Typography>
          <Typography sx={typographyStyle}>
            {account.availability} &#8364;
          </Typography>
          <Typography sx={typographyStyle}>
            ({getAccountOutcome().toFixed(2)} &#8364;)
          </Typography>
        </div>
      </Stack>
    </>
  );
}
