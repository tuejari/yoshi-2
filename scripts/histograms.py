import matplotlib.pyplot as plt
import pandas as pd
import seaborn as sns 
import numpy as np

data = pd.read_csv('E:\\Users\\Gebruiker\\surfdrive\\Documents\\Histogram-Analysis\\characteristics-analysis.csv', delimiter=",")

# Source outlier computation (unused): https://www.thoughtco.com/what-is-an-outlier-3126227 
mp = 3

x1 = data['Dispersion']
# q75, q25 = np.percentile(x1, [75 ,25])
# iqr = q75 - q25
# x1 = x1[x1 > q25-mp*iqr]
# x1 = x1[x1 < q75+mp*iqr]

x2 = data['BuggedFormality']
# q75, q25 = np.percentile(x2, [75 ,25])
# iqr = q75 - q25
# x2 = x2[x2 > q25-mp*iqr]
# x2 = x2[x2 < q75+mp*iqr]

x3 = data['Engagement']
# q75, q25 = np.percentile(x3, [75 ,25])
# iqr = q75 - q25
# x3 = x3[x3 > q25-mp*iqr]
# x3 = x3[x3 < q75+mp*iqr]

x4 = data['Longevity']
# q75, q25 = np.percentile(x4, [75 ,25])
# iqr = q75 - q25
# x4 = x4[x4 > q25-mp*iqr]
# x4 = x4[x4 < q75+mp*iqr]

# Plot
plt.figure(1)
x_array, y_array  = sns.histplot(data=x1, kde=True, bins=5).get_lines()[0].get_data() # Threshold dichterbij 1500 km
min_idx = np.argmin(y_array)
new_threshold = x_array[min_idx]
print(new_threshold)
plt.xlabel("Dispersion (km)")
plt.xlim(0,5000)
plt.ylim(0,25)
plt.grid(b=True, axis='y')
plt.axvline(x=4926, label="Old Threshold", color="magenta")
plt.axvline(x=new_threshold, label="New Threshold", color="orangered")
plt.legend()
plt.figure(2)
x_array, y_array = sns.histplot(data=x2, kde=True, bins=5).get_lines()[0].get_data() # no clear 2 thresholds
max_idx = np.argmax(y_array)
new_threshold = x_array[max_idx]
print(new_threshold)
# plt.axvline(x=new_threshold, label="New Threshold", color="orangered")
plt.xlabel("Formality Level")
plt.xlim(0,450)
plt.ylim(0,25)
plt.grid(b=True, axis='y')
plt.axvline(x=0.1, label="Old Low Threshold", linewidth=4, color="magenta")
plt.axvline(x=20, label="Old High Threshold", color="lime")
plt.legend()
plt.figure(3)
x_array, y_array = sns.histplot(data=x3, kde=True, bins=5).get_lines()[0].get_data() # Threshold bij 3.15 misschien beter
max_idx = np.argmax(y_array)
new_threshold = x_array[max_idx]
print(new_threshold)
plt.xlabel("Engagement Level")
plt.xlim(0,22)
plt.ylim(0,25)
plt.grid(b=True, axis='y')
plt.axvline(x=3.5, label="Threshold", color="magenta")
# plt.axvline(x=new_threshold, label="New Threshold", color="orangered")
plt.legend()
plt.figure(4)
sns.histplot(data=x4, kde=True, bins=5) # Ergens voor 200
plt.xlabel("Longevity (days)")
plt.xlim(0,1800)
plt.ylim(0,25)
plt.grid(b=True, axis='y')
plt.axvline(x=93, label="Threshold", color="magenta")
plt.legend()
plt.show()