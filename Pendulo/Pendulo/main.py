import io
import json
import math

filename = 'test2.txt'
kInf = 1000000009
kLen = 1.2

# maxSignalLength is range size for sensor action
maxSignalLength = 50

# returns a list of times when the sensor is activated
# times in ms, origin = 0
def getParsedList():
	with open(filename) as f:
		s = f.read()
	f.close()
	a = s.split('\n')
	
	del a[len(a) - 1]
	a = [int(k) for k in a]

	return a
	
def arithmeticMean(x):
	a = 0.0
	for k in x:
		a = a + k
	return a / len(x)
	
def geometricMean(x):
	a = 1.0
	for k in x:
		a = a * k
	return a ** (1.0 / len(x))
	
# cleans up noise in raw sensor readings, applying
# geometric mean, or harmonic mean
def pointReducer(p):
	mp = {}
	global kInf
	
	for k in p:
		found = False
		for x in mp.keys():
			if k >= x[0] and k <= x[1]:
				mp[x].append(k)
				found = True
				
		if found == False:
			mp[(k, k + maxSignalLength)] = [k]
			
	for x in sorted(mp):
		s = ''
		lo = kInf
		hi = -kInf
		for k in mp[x]:
			s = s + str(k) + ' '
			lo = min(lo, k)
			hi = max(hi, k)
		
	fp = []
	for x in sorted(mp):
		fp.append(arithmeticMean(mp[x]))
		
	it = 0
	diffs = []
	while it < len(fp):
		if it + 2 < len(fp):
			diffs.append(fp[it + 2] - fp[it])
			it = it + 2
		else:
			break
	
	fn = arithmeticMean(diffs)
	fn = fn / 1000
	# printing period
	bf = ''
	bf = bf + str(fn) + '\n'
	g = kLen * 4 * math.pi ** 2
	g = g / (fn ** 2)
	# printing gravity
	bf = bf + str(g) + '\n'
	
	with open('output.txt', 'w+') as f:
		f.write(bf)
	f.close()

def main():
	times = getParsedList()
	times = pointReducer(times)

if __name__ == '__main__':
	main()
