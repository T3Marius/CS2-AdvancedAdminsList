# CS2-AdvancedAdminsList
Advanced AdminsList plugin for cs2, this shows the admins in CenterHtmlMenu or chat, with his admin group too. The plugin automaticly locate the admin_groups file and get the groups from there.

it has one command = !adminslist

Config model:
```js
{

  "UseCenterHtmlMenu": true,
  
  "ImmunityFlag": "@css/root",
  
  "ShowYourSelf": true,
  
  "ShowFlag": "@css/generic",
  
  "ShowAdminGroups": true,
  
  "ConfigVersion": 1
  
}
```

Found out the path isn't the same for all people to admin_groups.json, so i added this in config:

"AdminGroupsFilePath": "/home/*******/csgo/addons/counterstrikesharp/configs/admin_groups.json",

After the /home/, you need to put your correct path name there, mine for example was /container/ cuz it was just a test server.

you can find your correct path when the server starts, in console:

![Photo](https://github.com/user-attachments/assets/ef3ee105-05c5-46d2-ba6f-2ac63e7910ec)

If you want to support me: revolut.me/dumitrqxrj
