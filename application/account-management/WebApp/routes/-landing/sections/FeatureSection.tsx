const createAccountUrl = "https://platformplatformgithub.blob.core.windows.net/create-account.png";
const enterCodeUrl = "https://platformplatformgithub.blob.core.windows.net/enter-code.png";
const profileEditDarkUrl = "https://platformplatformgithub.blob.core.windows.net/profile-edit-dark.png";
const profileEditLightUrl = "https://platformplatformgithub.blob.core.windows.net/profile-edit.png";
const editProfileUrl = "https://platformplatformgithub.blob.core.windows.net/edit-profile.png";
const accountSettingsUrl = "https://platformplatformgithub.blob.core.windows.net/account-setting.png";
const usersUrl = "https://platformplatformgithub.blob.core.windows.net/users.png";
const darkModeUrl = "https://platformplatformgithub.blob.core.windows.net/dark-mode.png";
const lightModeUrl = "https://platformplatformgithub.blob.core.windows.net/light-mode.png";

// FeatureSection: A functional component that displays the feature section
export function FeatureSection() {
  return (
    <div className="flex flex-col gap-8 text-center bg-white pb-12">
      <div className="flex flex-col gap-8 self-center   md:w-1/2 px-8 pt-24">
        <h1 className="text-amber-600 text-base font-semibold">FEATURES</h1>
        <h2 className="text-gray-900 text-2xl md:text-6xl font-semibold">
          All-in-one Infrastructure for any business
        </h2>
        <p className="text-slate-600 text-md md:text-xl font-normal">
          Get a full enterprise grade production environment running within 1
          hour. From development, infrastructure as code to fully deployed
          production application with a security score of 100%.
        </p>
      </div>
      <div className="flex flex-row justify-center gap-4 px-16 md:pr-0">
        <div className="flex flex-col gap-4">
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={createAccountUrl}
            alt="Create Account"
          />
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={enterCodeUrl}
            alt="Enter Code"
          />
        </div>
        <div className="flex flex-col gap-4">
          <div className="flex flex-row gap-2">
            <img
              className="shadow-xl shadow-gray-400 rounded-lg"
              src={profileEditDarkUrl}
              alt="Profile Edit Dark"
            />
            <div>
              <img
                className="shadow-xl shadow-gray-400 rounded-lg"
                src={darkModeUrl}
                alt="Dark Mode"
              />
            </div>
            <div>
              <img
                className="shadow-xl shadow-gray-400 rounded-lg"
                src={lightModeUrl}
                alt="Light Mode"
              />
            </div>
          </div>
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={editProfileUrl}
            alt="Edit Profile"
          />
        </div>
        <div className="flex-col gap-4 hidden md:flex">
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={accountSettingsUrl}
            alt="Account Settings"
          />
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={profileEditLightUrl}
            alt="Edit Profile Light"
          />
        </div>
        <div className="flex-col gap-4 hidden md:flex">
          <img
            className="shadow-xl shadow-gray-400 rounded-lg"
            src={usersUrl}
            alt="Users"
          />
        </div>
      </div>
    </div>
  );
}
