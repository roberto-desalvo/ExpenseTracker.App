import { Stack } from "@mui/material";

export default function HomeHeader() {
  return (
    <>
      <header className="h-1/8 text-white flex items-center py-4 bg-gray-900 shadow-xl">
        <Stack
          direction="row"
          spacing={2}
          alignItems="center"
          sx={{ padding: "0 0.5rem" }}
        >
          <h1 className="text-lime-500 text-2xl font-bold px-6">
            Expense Tracker
          </h1>
        </Stack>
      </header>
    </>
  );
}
