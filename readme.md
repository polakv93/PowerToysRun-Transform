# Transform Plugin for PowerToys Run

A [PowerToys Run](https://aka.ms/PowerToysOverview_PowerToysRun) plugin for run defined transformations on specific file.

## Installation

1. Download the latest release of the from the releases page.
2. Extract the zip file's contents to `%LocalAppData%\Microsoft\PowerToys\PowerToys Run\Plugins`
3. Restart PowerToys.
4. Go to `PowerToys Settings` > `System Tools` > `PowerToys Run` > `Plugins` > `Transform` and set value of `Directory with transformations` to the path of the directory with your transformations.

## Usage

1. Prepare directory with defined transformations.
```
- <Directory with transformations>
    - file1.json
    - file2.json
    - ...
```
where each *.json file contains JSON transformation with special key `__target` on root level example:
```json
{
    "__target": "path/to/target/file.json",
    ...rest of your json transformations
} 
```
1. Open PowerToys Run (default shortcut is <kbd>Alt+Space</kbd>.
1. Type `trans`.
1. Use arrows to select transformation and <kbd>Enter</kbd> to apply.