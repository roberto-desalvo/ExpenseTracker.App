import { Checkbox, Stack, Typography } from "@mui/material";
import { KeyboardArrowLeft, KeyboardArrowRight } from "@mui/icons-material";
import { useTableContext } from "../stores/TableContext";

export default function TableAside() {
  const tableContext = useTableContext();

  const incrementMonth = () => {
    const newDate = new Date(tableContext.filterDate);
    newDate.setMonth(newDate.getMonth() + 1);
    tableContext.modifyFilterDate(newDate);
  };

  const decrementMonth = () => {
    const newDate = new Date(tableContext.filterDate);
    newDate.setMonth(newDate.getMonth() - 1);
    tableContext.modifyFilterDate(newDate);
  };

  const keyboardArrowStyle = {
    color: "#cddc39",
    transition: "all 0.3s ease", 
    "&:hover": {
      cursor: "pointer",
      color: "#424242",
      backgroundColor: "rgba(192, 233, 89, 0.5)", 
      borderRadius: "50%", 
      boxShadow: "0 0 15px 5px rgba(192, 233, 89, 0.4)", 
    },
  }

  return (
    <aside className="w-1/5 bg-gray-800 px-4">
      <Stack direction="column" spacing={2} justifyContent="center">
        <Stack
          className="flex h-12 border"
          direction="row"
          spacing={2}
          sx={{
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <KeyboardArrowLeft
            sx={keyboardArrowStyle}
            onClick={() => decrementMonth()}
          />
          <h1 className="text-lime-500 text-xl uppercase font-bold">
            {tableContext.filterDate.toLocaleDateString("en-EN", {
              year: "numeric",
              month: "short",
            })}
          </h1>
          <KeyboardArrowRight
            sx={keyboardArrowStyle}
            onClick={() => incrementMonth()}
          />
        </Stack>
        <Stack
          className="flex h-12 border"
          direction="row"
          spacing={0}
          sx={{
            alignItems: "center",
            justifyContent: "center",
          }}
        >
          <Checkbox
            onChange={() => tableContext.toggleIncludeMoneyTransfers()}
            checked={tableContext.includeMoneyTransfers}
            sx={{
              color: "#cddc39",
              "&.Mui-checked": {
                color: "#cddc39", 
              },
            }}
          />
          <Typography sx={{ color: "#cddc39" }}>Include transfers</Typography>
        </Stack>
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
        <div className="flex items-center w-100 h-12 p-2 border" />
      </Stack>

      {/* <div className="items-center">
        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          justifyContent="center"
        >
          <IconButton
            size="small"
            sx={{ background: "grey" }}
            onClick={() => decrementMonth()}
          >
            <KeyboardArrowLeft />
          </IconButton>

          <h1 className="text-lime-500 text-2xl font-bold min-w-44">
            {filterContext.filterDate.toLocaleDateString("en-EN", {
              year: "numeric",
              month: "long",
            })}
          </h1>

          <IconButton
            size="small"
            sx={{ background: "grey" }}
            onClick={() => incrementMonth()}
          >
            <KeyboardArrowRight />
          </IconButton>
        </Stack>

        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          justifyContent="center"
        >
          <Checkbox
            onChange={() => filterContext.toggleIncludeMoneyTransfers()}
            checked={filterContext.includeMoneyTransfers}
          />
          <Typography>Include transfers</Typography>
        </Stack>
      </div> */}
    </aside>
  );
}
