using SecureComm;
using SecureComm.Screens;

ScreenManager screenMangager = new ScreenManager();
screenMangager.Navigate(new EnterGuidScreen());
await screenMangager.ManageScreens();