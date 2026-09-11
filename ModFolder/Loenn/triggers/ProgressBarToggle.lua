local item = {}

item.name = "CorreWithCare/ProgressBarToggle"

item.placements = {
	name = "normal",
	data = {
		mode = 1,
		target = "default",
		isCounter = true,
		showFor = 1,
	},
}

item.fieldInformation = {
	mode = {
		options = {
			["Show"] = 0,
			["Appear"] = 1,
			["Disappear"] = 2,
		},
		editable = false,
	}
}

return item