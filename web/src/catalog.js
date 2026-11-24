// Care values (ISO 3758). Keys must match the C# enums in api/Data.cs.
export const CARE = {
  wash: {
    label: 'Washing',
    options: {
      W30: '30 °C', W30Mild: '30 °C mild', W30VeryMild: '30 °C very mild',
      W40: '40 °C', W40Mild: '40 °C mild', W40VeryMild: '40 °C very mild',
      W50: '50 °C', W50Mild: '50 °C mild', W60: '60 °C', W60Mild: '60 °C mild',
      W70: '70 °C', W95: '95 °C', Hand: 'Hand wash', DoNot: 'Do not wash',
    },
  },
  bleach: { label: 'Bleaching', options: { Any: 'Any bleach', NonChlorine: 'Oxygen / non-chlorine only', DoNot: 'Do not bleach' } },
  iron: { label: 'Ironing', options: { Low: 'Low (110 °C)', Medium: 'Medium (150 °C)', High: 'High (200 °C)', DoNot: 'Do not iron' } },
  dryClean: {
    label: 'Dry cleaning',
    options: {
      P: 'P – perchloroethylene', PMild: 'P – mild', F: 'F – hydrocarbon', FMild: 'F – mild',
      W: 'W – wet clean', WMild: 'W – mild', WVeryMild: 'W – very mild', DoNot: 'Do not dry clean',
    },
  },
  tumbleDry: { label: 'Tumble drying', options: { Normal: 'Normal', Low: 'Low heat', DoNot: 'Do not tumble dry' } },
}

// Schmetz colour code. null = no colour band for that type.
export const NEEDLE_TYPES = {
  Universal: null, Stretch: '#f7d917', Jersey: '#f07818', Jeans: '#005fa0', Leather: '#a36b35',
  Microtex: '#5b4886', Quilting: '#00977a', Embroidery: '#c0404a', Metallic: '#f08a96', Topstitch: '#6cc3c4',
  Hemstitch: null, 'Double Eye': null, 'Quick Threading': null, Special: '#e6e6e6', Overlock: '#1d1d1b',
}

// NM size -> [US size, colour]
export const NEEDLE_SIZES = {
  60: [8, '#6cc3c4'], 65: [9, '#e6e6e6'], 70: [10, '#00977a'], 75: [11, '#f08a96'], 80: [12, '#f07818'],
  90: [14, '#005fa0'], 100: [16, '#5b4886'], 110: [18, '#f7d917'], 120: [19, '#a36b35'], 130: [21, '#c0404a'],
}

export const PROJECT_STATUS = { Planned: 'Planned', InProgress: 'In progress', Done: 'Done' }
