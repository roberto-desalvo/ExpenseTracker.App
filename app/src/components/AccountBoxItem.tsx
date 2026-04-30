import { Typography } from "@mui/material";
import Account from "../models/Account.tsx";
import Stack from "@mui/material/Stack";
import { useTableContext } from "../stores/TableContext.tsx";

interface AccountBoxItemProps {
  account: Account;
}

export default function AccountBoxItem({ account }: AccountBoxItemProps) {
  const tableContext = useTableContext();
  const isSelected = tableContext.selectedAccountIds.includes(account.id);

  const handleClick = () => {
    if (isSelected) {
      tableContext.modifySelectedAccountIds(
        tableContext.selectedAccountIds.filter(
          (selectedAccountId) => selectedAccountId !== account.id
        )
      );
    } else {
      tableContext.modifySelectedAccountIds([
        ...tableContext.selectedAccountIds,
        account.id,
      ]);
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
            ({getAccountOutcome().toFixed(2)} &#8364;)
          </Typography>
        </div>
      </Stack>
    </>
  );
}
